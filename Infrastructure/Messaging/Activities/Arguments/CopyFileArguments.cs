namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record CopyFileArguments(
        string SourceBucketName,
        string SourceKeyName,
        string DestinationBucketName,
        string DestinationKeyName);
}