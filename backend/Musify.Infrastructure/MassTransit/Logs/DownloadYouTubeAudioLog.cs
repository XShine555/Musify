namespace Musify.Infrastructure.MassTransit.Logs
{
    public record DownloadYouTubeAudioLog(
        string VideoId,
        string DestinationFilePath);
}
