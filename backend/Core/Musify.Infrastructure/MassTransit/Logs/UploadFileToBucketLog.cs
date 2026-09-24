namespace Musify.Infrastructure.MassTransit.Logs
{
    public record UploadFileToBucketLog(string DestinationBucket, string DestinationKey);
}
