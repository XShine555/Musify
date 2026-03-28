namespace Musify.Application.Events
{
    public record RemoveFileEvent(
        string Bucket,
        string Key);
}