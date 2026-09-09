namespace Musify.Application.Contracts
{
    public record YouTubeDownloadResult(int ExitCode, string StandardError);

    public interface IYouTubeDownloaderService
    {
        Task<YouTubeDownloadResult> DownloadAudioAsync(string videoId, string destinationFilePath, CancellationToken cancellationToken);
    }
}
