namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record RemoveFileFromBucketArguments(
        string BucketName,
        string KeyName);
}