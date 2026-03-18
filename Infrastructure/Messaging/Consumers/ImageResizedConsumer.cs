using Ardalis.Result;
using MassTransit;
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
                var upload = await database.Uploads.FindAsync(consumeContext.Message.UploadId);

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

                var fileResult = await storageHandler.GetFileAsync(
                    upload.BucketName,
                    upload.KeyName,
                    consumeContext.CancellationToken);

                if (fileResult.IsNotFound())
                {
                    logger.LogWarning("File not found in storage: BucketName={BucketName}, KeyName={KeyName}", upload.BucketName, upload.KeyName);
                    await UpdateState(database, upload, UploadState.Failed, consumeContext.CancellationToken);
                    return;
                }

                using var fileStream = fileResult.Value;

                foreach (var resize in consumeContext.Message.ImageResizes)
                {
                    var newUpload = new Upload
                    {
                        BucketName = upload.BucketName,
                        KeyName = $"{resize.SaveOnRoute}/{upload.Id}.webp",
                        ContentType = "image/webp",
                        State = UploadState.Processing,
                        EntityId = upload.EntityId,
                        EntityType = upload.EntityType
                    };

                    await database.Uploads.AddAsync(newUpload, consumeContext.CancellationToken);

                    await database.SaveChangesAsync(consumeContext.CancellationToken);

                    var pictureResult = await pictureHandler.ResizePictureAsync(
                        fileStream,
                        resize.Width,
                        resize.Height,
                        consumeContext.CancellationToken);

                    if (!pictureResult.IsSuccess)
                    {
                        logger.LogError("Failed to resize picture: {ErrorMessage}", string.Join("; ", pictureResult.Errors));
                        await UpdateState(database, newUpload, UploadState.Failed, consumeContext.CancellationToken);
                        continue;
                    }

                    var saveResult = await storageHandler.UploadFileAsync(
                        pictureResult.Value,
                        "image/webp",
                        upload.BucketName,
                        $"{resize.SaveOnRoute}/{upload.Id}.webp",
                        consumeContext.CancellationToken);

                    if (!saveResult.IsSuccess)
                    {
                        logger.LogError("Failed to upload resized picture: {ErrorMessage}", string.Join("; ", saveResult.Errors));
                        await UpdateState(database, newUpload, UploadState.Failed, consumeContext.CancellationToken);
                        continue;
                    }

                    await UpdateState(database, newUpload, UploadState.Completed, consumeContext.CancellationToken);
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