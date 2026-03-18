using Musify.Application.Pictures.Contracts;

namespace Musify.Application.Pictures.Events
{
    public record PictureResizeEvent(
        Guid UploadId,
        PictureResize[] ImageResizes);
}