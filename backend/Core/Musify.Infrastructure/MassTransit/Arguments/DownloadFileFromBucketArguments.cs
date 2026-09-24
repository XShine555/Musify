namespace Musify.Infrastructure.MassTransit.Arguments;

public record DownloadFileFromBucketArguments(
    string Bucket,
    string Key,
    string DestinationFilePathVariable);
