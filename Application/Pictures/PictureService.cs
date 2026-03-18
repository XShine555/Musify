using Ardalis.Result;
using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Pictures.Commands.ResizePicture;
using Musify.Application.Pictures.Contracts;
using Musify.Application.Pictures.Events;
using Musify.Domain.Entities;

namespace Musify.Application.Pictures
{
    public class PictureService(IStorageHandler storageHandler, IDatabase database, IPublishEndpoint publishEndpoint,
        ILogger<ResizeCommandHandler> logger, StorageConfiguration storageConfiguration)
        : IPictureService
    {
        public async Task<Result<Guid>> ResizePictureAsync(EntityType entityType, Guid EntityId, string keyName, string contentType, Stream pictureStream,
            PictureResize[] pictureResizes, CancellationToken cancellationToken)
        {
            var pictureId = Guid.NewGuid();

            var upload = new Upload
            {
                Id = pictureId,
                KeyName = keyName,
                ContentType = contentType,
                BucketName = storageConfiguration.BucketName,
                EntityType = entityType,
                EntityId = EntityId,
                State = UploadState.Completed
            };

            await database.Uploads.AddAsync(upload, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            var uploadResult = await storageHandler.UploadFileAsync(
                pictureStream,
                contentType,
                storageConfiguration.BucketName,
                keyName,
                cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload the picture with ID {PictureId} to bucket {BucketName}.", pictureId, storageConfiguration.BucketName);

                upload.State = UploadState.Failed;
                database.Uploads.Update(upload);
                await database.SaveChangesAsync(cancellationToken);

                return Result.Error("Failed to upload the picture.");
            }

            await publishEndpoint.Publish(new PictureResizeEvent(pictureId, pictureResizes));

            return Result.Success();
        }
    }
}