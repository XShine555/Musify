using ErrorOr;

namespace Musify.Application.Contracts
{
    public record YouTubeSongResult(
        string VideoId,
        string Title,
        string Artist,
        string Album,
        int DurationSeconds,
        string ThumbnailUrl);

    public record YouTubeSearchResult(
        IReadOnlyList<YouTubeSongResult> Items,
        string ContinuationToken);

    public record YouTubeStreamInfo(string Url, int ExpiresInSeconds);

    public interface IYouTubeMusicService
    {
        Task<ErrorOr<YouTubeSearchResult>> SearchSongsAsync(string query, string continuationToken, CancellationToken cancellationToken);

        Task<ErrorOr<YouTubeStreamInfo>> GetAudioStreamAsync(string videoId, CancellationToken cancellationToken);
    }
}
