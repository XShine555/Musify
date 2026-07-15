using ErrorOr;
using Microsoft.Extensions.Caching.Memory;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models.Search;
using YouTubeMusicAPI.Models.Streaming;
using YouTubeMusicAPI.Pagination;

namespace Musify.Infrastructure.Services
{
    public class YouTubeMusicService(YouTubeConfiguration configuration, IMemoryCache cache) : IYouTubeMusicService
    {
        private readonly YouTubeMusicClient client = new(geographicalLocation: configuration.GeographicalLocation);

        private sealed record CachedStreamInfo(string Url, DateTime ExpiresAtUtc);

        public async Task<ErrorOr<YouTubeSearchResult>> SearchSongsAsync(string query, string continuationToken, CancellationToken cancellationToken)
        {
            PaginatedAsyncEnumerable<SearchResult>? paginator;
            if (string.IsNullOrEmpty(continuationToken))
            {
                paginator = client.SearchAsync(query, SearchCategory.Songs);
            }
            else if (!cache.TryGetValue(SearchCacheKey(continuationToken), out paginator) || paginator is null)
            {
                return Error.NotFound(description: "The search continuation has expired.");
            }

            IReadOnlyList<SearchResult> results;
            try
            {
                results = await paginator.FetchNextPageAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                return Error.Failure(description: $"YouTube Music search failed: {exception.Message}");
            }

            var items = results
                .OfType<SongSearchResult>()
                .Select(song => new YouTubeSongResult(
                    song.Id,
                    song.Name,
                    string.Join(", ", song.Artists.Select(artist => artist.Name)),
                    song.Album?.Name ?? string.Empty,
                    (int)song.Duration.TotalSeconds,
                    song.Thumbnails
                        .OrderByDescending(thumbnail => thumbnail.Width)
                        .Select(thumbnail => thumbnail.Url)
                        .FirstOrDefault() ?? string.Empty))
                .ToList();

            var nextToken = string.Empty;
            if (paginator.HasMore)
            {
                nextToken = Guid.NewGuid().ToString("N");
                cache.Set(
                    SearchCacheKey(nextToken),
                    paginator,
                    TimeSpan.FromSeconds(configuration.SearchContinuationCacheSeconds));
            }

            return new YouTubeSearchResult(items, nextToken);
        }

        public async Task<ErrorOr<YouTubeStreamInfo>> GetAudioStreamAsync(string videoId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            if (cache.TryGetValue(StreamCacheKey(videoId), out CachedStreamInfo? cached)
                && cached is not null
                && cached.ExpiresAtUtc > now)
            {
                return new YouTubeStreamInfo(cached.Url, (int)(cached.ExpiresAtUtc - now).TotalSeconds);
            }

            StreamingData streamingData;
            try
            {
                streamingData = await client.GetStreamingDataAsync(videoId, cancellationToken);
            }
            catch (Exception exception)
            {
                return Error.Failure(description: $"Failed to resolve YouTube stream for '{videoId}': {exception.Message}");
            }

            var audioStream = streamingData.StreamInfo
                .OfType<AudioStreamInfo>()
                .OrderByDescending(stream => stream.Bitrate)
                .FirstOrDefault();

            if (audioStream is null || string.IsNullOrEmpty(audioStream.Url))
                return Error.NotFound(description: $"No audio stream available for '{videoId}'.");

            var expiresInSeconds = Math.Min(
                configuration.StreamUrlCacheSeconds,
                (int)streamingData.ExpiresIn.TotalSeconds);

            if (expiresInSeconds > 0)
            {
                cache.Set(
                    StreamCacheKey(videoId),
                    new CachedStreamInfo(audioStream.Url, now.AddSeconds(expiresInSeconds)),
                    TimeSpan.FromSeconds(expiresInSeconds));
            }

            return new YouTubeStreamInfo(audioStream.Url, expiresInSeconds);
        }

        private static string SearchCacheKey(string token) => $"yt-search:{token}";

        private static string StreamCacheKey(string videoId) => $"yt-stream:{videoId}";
    }
}
