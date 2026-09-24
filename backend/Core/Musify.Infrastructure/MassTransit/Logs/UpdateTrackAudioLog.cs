namespace Musify.Infrastructure.MassTransit.Logs
{
    public record UpdateTrackAudioLog(
        Guid TrackId,
        string? PreviousAudioFolderName,
        double PreviousDurationSeconds);
}
