using Musify.Application.Contracts.Application;

namespace Musify.Application.Events
{
    public record RemoveFileEvent(
        string BucketName,
        string KeyName);
}