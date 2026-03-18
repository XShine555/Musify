using Ardalis.Result;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Pictures.Events;
using Musify.Domain.Entities;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class ImageResizedConsumer(IStorageHandler storageHandler, IDatabase database, IPictureHandler pictureHandler, ILogger<ImageResizedConsumer> logger)
        : IConsumer<PictureResizeEvent>
    {
        public async Task Consume(ConsumeContext<PictureResizeEvent> consumeContext)
        {
            try
            {
                var upload = await database.Uploads.SingleOrDefaultAsync(u => u.Id == consumeContext.Message.UploadId);

                if (upload is null)
                {
                    logger.LogWarning("Upload not found in database: UploadId={UploadId}", consumeContext.Message.UploadId);
                    return;
                }

                if (upload.State != UploadState.Pending)
                {
                    logger.LogInformation("Upload is not in a pending state, skipping processing: UploadId={UploadId}, State={State}", consumeContext.Message.UploadId, upload.State);
                    return;
                }

                await UpdateState(database, upload, UploadState.Processing, consumeContext.CancellationToken);

                var file = await storageHandler.GetFileAsync(
                    upload.BucketName,
                    upload.KeyName,
                    consumeContext.CancellationToken);

                if (file.IsNotFound())
                {
                    logger.LogWarning("File not found in storage: BucketName={BucketName}, KeyName={KeyName}", upload.BucketName, upload.KeyName);

                    await UpdateState(database, upload, UploadState.Failed, consumeContext.CancellationToken);

                    return;
                }

                using var fileStream = file.Value;

                var pictureResult = await pictureHandler.ResizePictureAsync(
                    fileStream,
                    consumeContext.Message.Width,
                    consumeContext.Message.Height,
                    consumeContext.CancellationToken);

                if (!pictureResult.IsSuccess)
                {
                    string errors = string.Join("; ", pictureResult.Errors);
                    logger.LogError("Failed to resize picture: {ErrorMessage}", errors);
                    
                    await UpdateState(database, upload, UploadState.Failed, consumeContext.CancellationToken);

                    return;
                }

                var saveResult = await storageHandler.UploadFileAsync(
                    pictureResult.Value,
                    "image/webp",
                    upload.BucketName,
                    $"{consumeContext.Message.SaveRoute}/{upload.Id}.webp",
                    consumeContext.CancellationToken);

                if (!saveResult.IsSuccess)
                {
                    string errors = string.Join("; ", saveResult.Errors);
                    logger.LogError("Failed to upload resized picture: {ErrorMessage}", errors);

                    await UpdateState(database, upload, UploadState.Failed, consumeContext.CancellationToken);

                    return;
                }

                await UpdateState(database, upload, UploadState.Completed, consumeContext.CancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while consuming the ImageResizedConsumer message.");
                throw;
            }
        }

        async Task UpdateState(IDatabase database, Upload upload, UploadState uploadState, CancellationToken cancellationToken)
        {
            upload.State = uploadState;

            database.Uploads.Update(upload);

            await database.SaveChangesAsync(cancellationToken);
        }
    }
}