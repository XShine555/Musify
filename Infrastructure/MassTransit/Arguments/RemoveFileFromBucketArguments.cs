namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record RemoveFileFromBucketArguments(
        string BucketName,
        string KeyName);
}