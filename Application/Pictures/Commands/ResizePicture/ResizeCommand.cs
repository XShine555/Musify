using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Pictures.Contracts;

namespace Musify.Application.Pictures.Commands.ResizePicture
{
    public record ResizeCommand(
        string KeyName,
        string ContentType,
        Stream PictureStream,
        PictureResize[] PictureResizes)
        : IRequest<ResizeCommand, Result<Guid>>;
}