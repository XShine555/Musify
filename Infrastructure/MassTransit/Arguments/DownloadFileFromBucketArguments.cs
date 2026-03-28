namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record DownloadFileFromBucketArguments(
        string Bucket,
        string Key,
        string DestinationFilePathVariable);
}