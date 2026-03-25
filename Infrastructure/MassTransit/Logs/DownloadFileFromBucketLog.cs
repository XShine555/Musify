namespace Musify.Infrastructure.MassTransit.Activities.Logs
{
    public record DownloadFileFromBucketLog(
        string BucketName,
        string KeyName,
        string DestinationFilePath);
}