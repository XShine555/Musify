using ErrorOr;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.Services
{
    /// <summary>Stands in for <see cref="IYouTubeMusicService"/> when
    /// <see cref="Musify.Infrastructure.Configuration.YouTubeConfiguration.Enabled"/> is <c>false</c>,
    /// so the source can be turned off without a client ever being constructed. Every read fails
    /// with the same <see cref="ErrorType.Forbidden"/> error; callers already treat a failed
    /// YouTube call as "unavailable" (an HTTP error for direct requests, a skipped candidate for
    /// mix generation), so nothing downstream needs to know the source is disabled specifically.</summary>
    public sealed class DisabledYouTubeMusicService : IYouTubeMusicService
    {
        private static readonly Error DisabledError =
            Error.Forbidden(description: "The YouTube Music source is disabled.");

        public Task<ErrorOr<YouTubeSearchResult>> SearchSongsAsync(string query, string continuationToken, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<YouTubeSearchResult>>(DisabledError);

        public Task<ErrorOr<YouTubeAlbumSearchResult>> SearchAlbumsAsync(string query, string continuationToken, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<YouTubeAlbumSearchResult>>(DisabledError);

        public Task<ErrorOr<YouTubeAlbumDetail>> GetAlbumAsync(string albumId, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<YouTubeAlbumDetail>>(DisabledError);

        public Task<ErrorOr<YouTubeStreamInfo>> GetAudioStreamAsync(string videoId, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<YouTubeStreamInfo>>(DisabledError);

        public Task<ErrorOr<YouTubeSongResult>> GetSongAsync(string videoId, CancellationToken cancellationToken) =>
            Task.FromResult<ErrorOr<YouTubeSongResult>>(DisabledError);

        // Never reached in practice: every caller only reaches this after a successful search or
        // song lookup above, neither of which can succeed while disabled. Passing the URL through
        // unchanged is the safest thing to do if it's ever called anyway.
        public string ResolveArtworkUrl(string thumbnailUrl) => thumbnailUrl;
    }
}
