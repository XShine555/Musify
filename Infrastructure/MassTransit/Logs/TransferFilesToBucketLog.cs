namespace Musify.Infrastructure.MassTransit.Activities.Logs
{
    public record TransferFilesToBucketLog(string DestinationBucket, string[] UploadedKeys);
}
