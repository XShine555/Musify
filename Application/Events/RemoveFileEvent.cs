using Musify.Application.Contracts.Application;

namespace Musify.Application.Events
{
    public record RemoveFileEvent(
        Guid JobId,
        string BucketName,
        string KeyName) : IEvent;
}