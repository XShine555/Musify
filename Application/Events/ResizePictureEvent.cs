using Musify.Application.Contracts.Application;

namespace Musify.Application.Events
{
    public record ResizePictureEvent(
        Guid JobId,
        string BucketName,
        string KeyName,
        ResizePictureArguments[] Arguments) : IEvent;
}