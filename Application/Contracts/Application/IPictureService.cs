using Ardalis.Result;
using Musify.Application.Pictures.Contracts;

namespace Musify.Application.Contracts.Application
{
    public interface IPictureService
    {
        Task<Result<Guid>> ResizePictureAsync(string keyName, string contentType, Stream pictureStream, PictureResize[] pictureResizes,
            CancellationToken cancellationToken);
    }
}