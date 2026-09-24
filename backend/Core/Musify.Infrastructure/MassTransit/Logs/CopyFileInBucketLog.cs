namespace Musify.Infrastructure.MassTransit.Logs
{
    public record CopyFileInBucketLog(
        string DestinationBucket,
        string DestinationKey);
}
