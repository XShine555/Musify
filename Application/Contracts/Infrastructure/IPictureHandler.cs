using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IPictureHandler
    {
        public string FileExtension { get; }

        public string ContentType { get; }

        Task<Result<Stream>> ResizePictureAsync(Stream pictureStream, int width, int height, CancellationToken cancellationToken);
    }
}