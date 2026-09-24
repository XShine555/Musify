namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record RemoveFileFromBucketArguments(
        string Bucket,
        string Key);
}
