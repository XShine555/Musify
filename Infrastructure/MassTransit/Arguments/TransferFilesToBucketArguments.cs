namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record TransferFilesToBucketArguments(
        string DestinationBucketName,
        string SourceDirectoryVariableName,
        string? DestinationKeyName = null,
        string? DestinationKeyNameVariableName = null);
}