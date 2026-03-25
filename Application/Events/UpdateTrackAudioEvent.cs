namespace Musify.Application.Events
{
    public record UpdateTrackAudioEvent(
        Guid TrackId,
        string SourceBucketName,
        string SourceKeyName,
        string DestinationBucketName,
        string DestinationFolderKeyName);
}