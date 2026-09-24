namespace Musify.Application.Contracts
{
    public interface IPictureService
    {
        public Task<Stream> ResizePictureAsWebpAsync(Stream pictureStream, int width, int height, CancellationToken cancellationToken);
    }
}
