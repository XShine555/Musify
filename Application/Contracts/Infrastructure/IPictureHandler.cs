using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IPictureHandler
    {
        Task<Result<Stream>> ResizePictureAsync(Stream pictureStream, int width, int height, CancellationToken cancellationToken);
    }
}