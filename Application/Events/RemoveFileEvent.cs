namespace Musify.Application.Events
{
    public record RemoveFileEvent(
        string BucketName,
        string KeyName);
}