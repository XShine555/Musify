using ErrorOr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Helpers;
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

        private sealed record SearchPage(IReadOnlyList<SearchResult> Results, string NextToken);

        private async Task<ErrorOr<SearchPage>> FetchPageAsync(
            string query,
            SearchCategory category,
            string continuationToken,
            CancellationToken cancellationToken)
        {
            PaginatedAsyncEnumerable<SearchResult>? paginator;
            if (string.IsNullOrEmpty(continuationToken))
            {
                paginator = client.SearchAsync(query, category);
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
                logger.LogError(exception, "YouTube Music {Category} search failed for query '{Query}'", category, query);
                return Error.Failure(description: $"YouTube Music search failed: {exception.Message}");
            }

            var nextToken = string.Empty;
            if (paginator.HasMore)
            {
                nextToken = Guid.NewGuid().ToString("N");
                cache.Set(
                    SearchCacheKey(nextToken),
                    paginator,
                    TimeSpan.FromSeconds(configuration.SearchContinuationCacheSeconds));
            }

            return new SearchPage(results, nextToken);
        }

        private async Task<ErrorOr<YouTubeSearchResult>> FetchSearchPageAsync(string query, string continuationToken, CancellationToken cancellationToken)
        {
            var page = await FetchPageAsync(query, SearchCategory.Songs, continuationToken, cancellationToken);
            if (page.IsError)
                return page.Errors;

            var items = page.Value.Results
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

            return new YouTubeSearchResult(items, page.Value.NextToken);
        }

        public async Task<ErrorOr<YouTubeAlbumSearchResult>> SearchAlbumsAsync(string query, string continuationToken, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(continuationToken))
                return await FetchAlbumSearchPageAsync(query, continuationToken, cancellationToken);

            return await singleFlight.GetOrCreateAsync(
                AlbumSearchResultsCacheKey(query),
                TimeSpan.FromSeconds(configuration.SearchResultsCacheSeconds),
                () => FetchAlbumSearchPageAsync(query, continuationToken, cancellationToken));
        }

        private async Task<ErrorOr<YouTubeAlbumSearchResult>> FetchAlbumSearchPageAsync(string query, string continuationToken, CancellationToken cancellationToken)
        {
            var page = await FetchPageAsync(query, SearchCategory.Albums, continuationToken, cancellationToken);
            if (page.IsError)
                return page.Errors;

            var items = page.Value.Results
                .OfType<AlbumSearchResult>()
                .Select(ToAlbumResult)
                .ToList();

            return new YouTubeAlbumSearchResult(items, page.Value.NextToken);
        }

        public async Task<ErrorOr<YouTubeAlbumDetail>> GetAlbumAsync(string albumId, CancellationToken cancellationToken)
        {
            return await singleFlight.GetOrCreateAsync(
                AlbumCacheKey(albumId),
                TimeSpan.FromSeconds(configuration.SongInfoCacheSeconds),
                () => FetchAlbumAsync(albumId, cancellationToken));
        }

        private async Task<ErrorOr<YouTubeAlbumDetail>> FetchAlbumAsync(string albumId, CancellationToken cancellationToken)
        {
            try
            {
                var browseId = await client.GetAlbumBrowseIdAsync(albumId, cancellationToken);
                var info = await client.GetAlbumInfoAsync(browseId, cancellationToken);

                var album = new YouTubeAlbumResult(
                    albumId,
                    info.Name,
                    string.Join(", ", info.Artists.Select(artist => artist.Name)),
                    PickThumbnail(info.Thumbnails),
                    info.ReleaseYear,
                    info.IsSingle,
                    info.IsEp,
                    info.Artists
                        .Select(artist => new YouTubeArtistRef(
                            string.IsNullOrEmpty(artist.Id) ? null : artist.Id,
                            artist.Name))
                        .ToList());

                var tracks = info.Songs
                    .Select((song, index) => new YouTubeAlbumTrack(
                        song.Id ?? string.Empty,
                        song.Name,
                        (int)song.Duration.TotalSeconds,
                        song.SongNumber ?? index + 1,
                        song.IsExplicit))
                    .Where(track => !string.IsNullOrEmpty(track.VideoId))
                    .ToList();

                return new YouTubeAlbumDetail(
                    album,
                    StripWikipediaAttribution(info.Description),
                    (int)info.Duration.TotalSeconds,
                    tracks);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to load YouTube album '{AlbumId}'", albumId);
                return Error.Failure(description: $"Failed to load YouTube album '{albumId}': {exception.Message}");
            }
        }

        private static string StripWikipediaAttribution(string? description)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            var markerIndex = description.IndexOf("From Wikipedia", StringComparison.OrdinalIgnoreCase);
            var cleaned = markerIndex >= 0 ? description[..markerIndex] : description;
            return cleaned.TrimEnd();
        }

        private YouTubeAlbumResult ToAlbumResult(AlbumSearchResult album) =>
            new(
                album.Id,
                album.Name,
                string.Join(", ", album.Artists.Select(artist => artist.Name)),
                PickThumbnail(album.Thumbnails),
                album.ReleaseYear,
                album.IsSingle,
                album.IsEp,
                album.Artists
                    .Select(artist => new YouTubeArtistRef(
                        string.IsNullOrEmpty(artist.Id) ? null : artist.Id,
                        artist.Name))
                    .ToList());

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
                    info.Album?.Name ?? string.Empty,
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
            YouTubeThumbNailHelper.WithSize(thumbnailUrl, configuration.ArtworkSize);

        private string PickThumbnail(IEnumerable<YouTubeMusicAPI.Models.Thumbnail> thumbnails)
        {
            var ordered = thumbnails.OrderBy(thumbnail => thumbnail.Width).ToList();

            var square = ordered.Where(thumbnail => YouTubeThumbNailHelper.IsSquare(thumbnail.Url)).ToList();
            var candidates = square.Count > 0 ? square : ordered;

            var chosen = candidates.FirstOrDefault(thumbnail => thumbnail.Width >= configuration.ThumbnailSize)
                ?? candidates.LastOrDefault();

            return chosen is null
                ? string.Empty
                : YouTubeThumbNailHelper.WithSize(chosen.Url, configuration.ThumbnailSize);
        }

        private static string SearchCacheKey(string token) => $"yt-search:{token}";

        private static string SearchResultsCacheKey(string query) => $"yt-search-results:{query.Trim().ToLowerInvariant()}";

        private static string AlbumSearchResultsCacheKey(string query) => $"yt-album-search:{query.Trim().ToLowerInvariant()}";

        private static string AlbumCacheKey(string albumId) => $"yt-album:{albumId}";

        private static string SongInfoCacheKey(string videoId) => $"yt-song:{videoId}";

        private static string StreamCacheKey(string videoId) => $"yt-stream:{videoId}";
    }
}
