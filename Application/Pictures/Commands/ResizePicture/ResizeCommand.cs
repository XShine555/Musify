using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Pictures.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Pictures.Commands.ResizePicture
{
    public record ResizeCommand(
        EntityType EntityType,
        Guid EntityId,
        string KeyName,
        string ContentType,
        Stream PictureStream,
        PictureResize[] PictureResizes)
        : IRequest<ResizeCommand, Result<Guid>>;
}