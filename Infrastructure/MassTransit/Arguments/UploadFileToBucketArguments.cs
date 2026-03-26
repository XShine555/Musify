namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record UploadFileToBucketArguments(
        string SourceFilePath,
        string ContentType,
        string DestinationBucketName,
        string DestinationKeyName);
}