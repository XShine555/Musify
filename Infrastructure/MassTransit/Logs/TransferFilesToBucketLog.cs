namespace Musify.Infrastructure.MassTransit.Activities.Logs
{
    public record TransferFilesToBucketLog(string DestinationBucketName, string[] UploadedKeys);
}
