namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record ResizePictureArgument(
        string OriginalBucketName,
        string OriginalKeyName,
        string DestinationBucketName,
        string DestinationKeyName,
        int Width,
        int Height);
}