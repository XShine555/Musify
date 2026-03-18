using Ardalis.Result;
using Musify.Application.Pictures.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Contracts.Application
{
    public interface IPictureService
    {
        Task<Result<Guid>> ResizePictureAsync(EntityType entityType, Guid EntityId, string keyName, string contentType, Stream pictureStream,
                    PictureResize[] pictureResizes, CancellationToken cancellationToken);
    }
}