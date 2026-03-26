namespace Musify.Infrastructure.MassTransit.Logs
{
    public record UploadFileToBucketLog(string DestinationBucketName, string DestinationKeyName);
}