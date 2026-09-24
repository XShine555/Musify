namespace Musify.Infrastructure.MassTransit.Logs;

public record TransferFilesToBucketLog(string DestinationBucket, string[] UploadedKeys);
