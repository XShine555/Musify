namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record RemoveFolderFromBucketArguments(
        string Bucket,
        string FolderKeyVariable);
}
