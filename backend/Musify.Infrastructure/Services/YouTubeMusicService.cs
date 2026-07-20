using ErrorOr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models.Search;
using YouTubeMusicAPI.Models.Streaming;
using YouTubeMusicAPI.Pagination;

namespace Musify.Infrastructure.Services
{
    public class YouTubeMusicService(YouTubeConfiguration configuration, IMemoryCache cache, ILogger<YouTubeMusicService> logger) : IYouTubeMusicService
    {
        private readonly YouTubeMusicClient client = new(geographicalLocation: configuration.GeographicalLocation);
        private readonly SingleFlightCache singleFlight = new(cache);

        private sealed record CachedStreamInfo(string Url, DateTime ExpiresAtUtc);

        public async Task<ErrorOr<YouTubeSearchResult>> SearchSongsAsync(string query, string continuationToken, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(continuationToken))
                return await FetchSearchPageAsync(query, continuationToken, cancellationToken);

            return await singleFlight.GetOrCreateAsync(
                SearchResultsCacheKey(query),
                TimeSpan.FromSeconds(configuration.SearchResultsCacheSeconds),
                () => FetchSearchPageAsync(query, continuationToken, cancellationToken));
        }

        private async Task<ErrorOr<YouTubeSearchResult>> FetchSearchPageAsync(string query, string continuationToken, CancellationToken cancellationToken)
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
                logger.LogError(exception, "YouTube Music search failed for query '{Query}'", query);
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
                    PickThumbnail(song.Thumbnails),
                    song.IsExplicit,
                    song.Artists
                        .Select(artist => new YouTubeArtistRef(
                            string.IsNullOrEmpty(artist.Id) ? null : artist.Id,
                            artist.Name))
                        .ToList()))
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
                logger.LogError(exception, "Failed to resolve YouTube stream for '{VideoId}'", videoId);
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

        public async Task<ErrorOr<YouTubeSongResult>> GetSongAsync(string videoId, CancellationToken cancellationToken)
        {
            return await singleFlight.GetOrCreateAsync(
                SongInfoCacheKey(videoId),
                TimeSpan.FromSeconds(configuration.SongInfoCacheSeconds),
                () => FetchSongAsync(videoId, cancellationToken));
        }

        private async Task<ErrorOr<YouTubeSongResult>> FetchSongAsync(string videoId, CancellationToken cancellationToken)
        {
            try
            {
                var info = await client.GetSongVideoInfoAsync(videoId, cancellationToken);

                return new YouTubeSongResult(
                    videoId,
                    info.Name,
                    string.Join(", ", info.Artists.Select(artist => artist.Name)),
                    string.Empty,
                    (int)info.Duration.TotalSeconds,
                    PickThumbnail(info.Thumbnails),
                    info.IsExplicit,
                    info.Artists
                        .Select(artist => new YouTubeArtistRef(
                            string.IsNullOrEmpty(artist.Id) ? null : artist.Id,
                            artist.Name))
                        .ToList());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to load YouTube song info for '{VideoId}'", videoId);
                return Error.Failure(description: $"Failed to load YouTube song info for '{videoId}': {exception.Message}");
            }
        }

        public string ResolveArtworkUrl(string thumbnailUrl) =>
            YouTubeThumbnail.WithSize(thumbnailUrl, configuration.ArtworkSize);

        private string PickThumbnail(IEnumerable<YouTubeMusicAPI.Models.Thumbnail> thumbnails)
        {
            var ordered = thumbnails.OrderBy(thumbnail => thumbnail.Width).ToList();

            var chosen = ordered.FirstOrDefault(thumbnail => thumbnail.Width >= configuration.ThumbnailSize)
                ?? ordered.LastOrDefault();

            return chosen is null
                ? string.Empty
                : YouTubeThumbnail.WithSize(chosen.Url, configuration.ThumbnailSize);
        }

        private static string SearchCacheKey(string token) => $"yt-search:{token}";

        private static string SearchResultsCacheKey(string query) => $"yt-search-results:{query.Trim().ToLowerInvariant()}";

        private static string SongInfoCacheKey(string videoId) => $"yt-song:{videoId}";

        private static string StreamCacheKey(string videoId) => $"yt-stream:{videoId}";
    }
}
