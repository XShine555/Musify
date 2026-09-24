namespace Musify.Infrastructure.MassTransit.Arguments;

public record CopyFileInBucketArguments(
    string SourceBucket,
    string SourceKey,
    string DestinationBucket,
    string DestinationKey);
