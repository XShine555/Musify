namespace Musify.Application.Contracts
{
    public interface IPictureService
    {
        Task<Stream> ResizePictureAsWebpAsync(Stream pictureStream, int width, int height, CancellationToken cancellationToken);
    }
}