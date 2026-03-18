using Ardalis.Result;
using DispatchR.Abstractions.Send;
using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Pictures.Events;
using Musify.Domain.Entities;

namespace Musify.Application.Pictures.Commands.ResizeImage
{
    public class ResizeCommandHandler(IStorageHandler storageHandler, IDatabase database, IPublishEndpoint publishEndpoint,
        ILogger<ResizeCommandHandler> logger, StorageConfiguration storageConfiguration)
        : IRequestHandler<ResizeCommand, Task<Result<Guid> >>
    {
        public async Task<Result<Guid>> Handle(ResizeCommand request, CancellationToken cancellationToken)
        {
            var pictureId = Guid.NewGuid();

            var upload = new Upload
            {
                Id = pictureId,
                KeyName = $"{storageConfiguration.Routes.Uploads}/{pictureId}",
                ContentType = request.ContentType,
                BucketName = storageConfiguration.BucketName,
            };

            await database.Uploads.AddAsync(upload, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            var uploadResult = await storageHandler.UploadFileAsync(
                request.PictureStream,
                request.ContentType,
                storageConfiguration.BucketName,
                pictureId.ToString(),
                cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload the picture with ID {PictureId} to bucket {BucketName}.", pictureId, storageConfiguration.BucketName);
                return Result.Error("Failed to upload the picture.");
            }

            await publishEndpoint.Publish(new ImageResizeEvent(
                pictureId,
                request.Width,
                request.Height,
                request.SaveRoute),
                cancellationToken);

            return Result.Success();
        }
    }
}