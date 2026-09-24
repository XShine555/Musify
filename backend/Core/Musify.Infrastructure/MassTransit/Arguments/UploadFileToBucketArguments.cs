namespace Musify.Infrastructure.MassTransit.Arguments;

public record UploadFileToBucketArguments(
    string FilePathVariable,
    string DestinationBucket,
    string DestinationRoute);
