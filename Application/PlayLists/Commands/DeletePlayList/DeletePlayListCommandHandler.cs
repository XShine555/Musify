using Ardalis.Result;
using DispatchR.Abstractions.Send;
using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists;

namespace Musify.Application.PlayLists.Commands.DeletePlayList
{
    public class DeletePlayListCommandHandler(IDatabase database, IEventBus eventBus,
        ApplicationStorageConfiguration storageConfiguration, PlayListConfiguration playListConfiguration, ILogger<DeletePlayListCommandHandler> logger)
        : IRequestHandler<DeletePlayListCommand, Task<Result>>
    {
        public async Task<Result> Handle(DeletePlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.FindAsync(request.PlayListId, cancellationToken);

            if (playList is null)
            {
                logger.LogDebug("PlayList with id={PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogInformation("PlayList with id={PlayListId} does not belong to user with id={UserId}", request.PlayListId, request.UserId);
                return Result.Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(playList.OriginalPictureKeyName))
            {
                var originalImageStorageKey = Path.Join(playListConfiguration.Routes.OriginalPictures, playList.OriginalPictureKeyName);

                try
                {
                    await eventBus.PublishAsync(new RemoveFileEvent(
                       Guid.NewGuid(),
                       storageConfiguration.BucketName,
                       originalImageStorageKey), cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to publish remove file event for original picture of PlayList id={PlayListId}.", playList.Id);
                    return Result.Error($"Failed to publish remove file event for PlayList with id {playList.Id}.");
                }

                if (playList.SmallPictureKeyName != playListConfiguration.Routes.PresetSmallPicture)
                {
                    try
                    {
                        await eventBus.PublishAsync(new RemoveFileEvent(
                            Guid.NewGuid(),
                            storageConfiguration.BucketName,
                            playList.SmallPictureKeyName), cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to publish remove file event for small picture of PlayList id={PlayListId}.", playList.Id);
                        return Result.Error($"Failed to publish remove file event for PlayList with id {playList.Id}.");
                    }
                }

                if (playList.MediumPictureKeyName != playListConfiguration.Routes.PresetMediumPicture)
                {
                    try
                    {
                        await eventBus.PublishAsync(new RemoveFileEvent(
                            Guid.NewGuid(),
                            storageConfiguration.BucketName,
                            playList.MediumPictureKeyName), cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to publish remove file event for medium picture of PlayList id={PlayListId}.", playList.Id);
                        return Result.Error($"Failed to publish remove file event for PlayList with id {playList.Id}.");
                    }
                }

                if (playList.LargePictureKeyName != playListConfiguration.Routes.PresetLargePicture)
                {
                    try
                    {
                        await eventBus.PublishAsync(new RemoveFileEvent(
                            Guid.NewGuid(),
                            storageConfiguration.BucketName,
                            playList.LargePictureKeyName), cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Failed to publish remove file event for large picture of PlayList id={PlayListId}.", playList.Id);
                        return Result.Error($"Failed to publish remove file event for PlayList with id {playList.Id}.");
                    }
                }
            }

            database.PlayLists.Remove(playList);
            await database.SaveChangesAsync(cancellationToken);
            return Result.NoContent();
        }
    }
}