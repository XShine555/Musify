namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record TransferFilesToBucketArguments(
        string DestinationBucket,
        string SourceDirectoryVariable,
        string DestinationKey);
}