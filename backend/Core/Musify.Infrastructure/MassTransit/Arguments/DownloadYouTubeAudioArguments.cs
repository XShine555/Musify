namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record DownloadYouTubeAudioArguments(
        Guid TrackId,
        string VideoId,
        string DestinationFilePathVariable);
}
