namespace Musify.Infrastructure.Messaging.Activities.Logs
{
    public record TransferFilesToBucketLog(string DestinationBucketName, string[] UploadedKeys);
}
