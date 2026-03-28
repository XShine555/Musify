namespace Musify.Application.Events
{
    public record UpdateTrackAudioEvent(
        Guid TrackId,
        string SourceBucket,
        string SourceKey,
        string DestinationBucket,
        string DestinationFolderKey);
}