using Ardalis.Result;
using DispatchR.Abstractions.Send;

namespace Musify.Application.Pictures.Commands.ResizeImage
{
    public record ResizeCommand(
        Stream PictureStream,
        string ContentType,
        int Width,
        int Height,
        string SaveRoute)
        : IRequest<ResizeCommand, Result<Guid>>;
}