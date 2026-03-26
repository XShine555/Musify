namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record UploadFileToBucketArguments(
        string FilePathVariableName,
        string ContentType,
        string DestinationBucketName,
        string DestinationKeyName);
}