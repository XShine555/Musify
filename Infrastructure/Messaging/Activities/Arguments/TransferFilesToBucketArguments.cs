namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record TransferFilesToBucketArguments(
        string DestinationBucketName,
        string DestinationKeyName);
}