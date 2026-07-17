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

    public record YouTubeStreamInfo(string Url, int ExpiresInSeconds);

    public interface IYouTubeMusicService
    {
        Task<ErrorOr<YouTubeSearchResult>> SearchSongsAsync(string query, string continuationToken, CancellationToken cancellationToken);

        Task<ErrorOr<YouTubeStreamInfo>> GetAudioStreamAsync(string videoId, CancellationToken cancellationToken);

        Task<ErrorOr<YouTubeSongResult>> GetSongAsync(string videoId, CancellationToken cancellationToken);
    }
}
