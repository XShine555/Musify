using ErrorOr;

namespace Musify.Application.Contracts
{
    public record YouTubeArtistRef(string? Id, string Name);

    public record YouTubeSongResult(
        string VideoId,
        string Title,
        string Artist,
        string Album,
        int DurationSeconds,
        string ThumbnailUrl,
        bool IsExplicit,
        IReadOnlyList<YouTubeArtistRef> Artists);

    public record YouTubeSearchResult(
        IReadOnlyList<YouTubeSongResult> Items,
        string ContinuationToken);

    public record YouTubeAlbumResult(
        string AlbumId,
        string Title,
        string Artist,
        string ThumbnailUrl,
        int? ReleaseYear,
        bool IsSingle,
        bool IsEp,
        IReadOnlyList<YouTubeArtistRef> Artists);

    public record YouTubeAlbumSearchResult(
        IReadOnlyList<YouTubeAlbumResult> Items,
        string ContinuationToken);

    public record YouTubeAlbumTrack(
        string VideoId,
        string Title,
        int DurationSeconds,
        int TrackNumber,
        bool IsExplicit);

    public record YouTubeAlbumDetail(
        YouTubeAlbumResult Album,
        string Description,
        int TotalDurationSeconds,
        IReadOnlyList<YouTubeAlbumTrack> Tracks);

    public record YouTubeStreamInfo(string Url, int ExpiresInSeconds);

    public interface IYouTubeMusicService
    {
        Task<ErrorOr<YouTubeSearchResult>> SearchSongsAsync(string query, string continuationToken, CancellationToken cancellationToken);

        Task<ErrorOr<YouTubeAlbumSearchResult>> SearchAlbumsAsync(string query, string continuationToken, CancellationToken cancellationToken);

        Task<ErrorOr<YouTubeAlbumDetail>> GetAlbumAsync(string albumId, CancellationToken cancellationToken);

        Task<ErrorOr<YouTubeStreamInfo>> GetAudioStreamAsync(string videoId, CancellationToken cancellationToken);

        Task<ErrorOr<YouTubeSongResult>> GetSongAsync(string videoId, CancellationToken cancellationToken);

        string ResolveArtworkUrl(string thumbnailUrl);
    }
}
