namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record RemoveFileArguments(
        string BucketName,
        string KeyName);
}