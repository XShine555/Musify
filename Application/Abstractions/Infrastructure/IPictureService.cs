namespace Musify.Application.Abstractions.Infrastructure
{
    public interface IPictureService
    {
        Task<Stream> ResizePictureAsWebpAsync(Stream pictureStream, int width, int height, CancellationToken cancellationToken);
    }
}