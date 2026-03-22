namespace Musify.Infrastructure.Messaging.Activities.Logs
{
    public record DownloadFileFromBucketLog(
        string BucketName,
        string KeyName,
        string DestinationPath);
}