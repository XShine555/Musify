namespace Musify.Infrastructure.MassTransit.Activities.Logs
{
    public record DownloadFileFromBucketLog(
        string Bucket,
        string Key,
        string DestinationFilePath);
}