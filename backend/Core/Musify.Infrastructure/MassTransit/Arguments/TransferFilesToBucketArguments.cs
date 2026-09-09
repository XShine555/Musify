namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record TransferFilesToBucketArguments(
        string DestinationBucket,
        string SourceDirectoryVariable,
        string DestinationKey);
}