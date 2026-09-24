namespace Musify.Infrastructure.MassTransit.Logs
{
    public record DownloadFileFromBucketLog(
        string Bucket,
        string Key,
        string DestinationFilePath);
}
