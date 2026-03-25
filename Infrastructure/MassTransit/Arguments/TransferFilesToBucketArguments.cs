namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record TransferFilesToBucketArguments(
        string DestinationBucketName,
        string SourceDirectoryVariableName,
        string? DestinationKeyName = null,
        string? DestinationKeyNameVariableName = null);
}