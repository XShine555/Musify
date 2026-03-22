namespace Musify.Application.Events
{
    public record TranscodeAudioFromTrackEvent(
        string SourceBucketName,
        string SourceKeyName,
        string DestinationBucketName,
        string DestinationKeyName);
}