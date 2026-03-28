namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record RemoveFileFromBucketArguments(
        string Bucket,
        string Key);
}