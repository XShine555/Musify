namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record DownloadFileFromBucketArguments(
        string BucketName,
        string KeyName,
        string DestinationPath);
}