namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record TransferFilesToBucketArguments(
        string FolderPath,
        string DestinationBucketName,
        string DestinationKeyName);
}