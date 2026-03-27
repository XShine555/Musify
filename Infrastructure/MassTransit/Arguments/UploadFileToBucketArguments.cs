namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record UploadFileToBucketArguments(
        string FilePathVariableName,
        string DestinationBucketName,
        string DestinationRoute);
}