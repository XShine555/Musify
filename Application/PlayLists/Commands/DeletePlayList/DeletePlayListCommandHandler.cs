using Ardalis.Result;
using DispatchR.Abstractions.Send;
using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;

namespace Musify.Application.PlayLists.Commands.DeletePlayList
{
    public class DeletePlayListCommandHandler(IDatabase database, IPublishEndpoint publishEndpoint,
        StorageConfiguration storageConfiguration, PlayListConfiguration playListConfiguration, ILogger<DeletePlayListCommandHandler> logger)
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

            await publishEndpoint.Publish(new RemoveFileEvent(
                storageConfiguration.BucketName,
                playList.OriginalPictureKeyName), cancellationToken);

            if (playList.SmallPictureKeyName != playListConfiguration.Routes.PresetSmallPicture)
                await publishEndpoint.Publish(new RemoveFileEvent(
                    storageConfiguration.BucketName,
                    playList.SmallPictureKeyName), cancellationToken);

            if (playList.MediumPictureKeyName != playListConfiguration.Routes.PresetMediumPicture)
                await publishEndpoint.Publish(new RemoveFileEvent(
                    storageConfiguration.BucketName,
                    playList.MediumPictureKeyName), cancellationToken);

            if (playList.LargePictureKeyName != playListConfiguration.Routes.PresetLargePicture)
                await publishEndpoint.Publish(new RemoveFileEvent(
                    storageConfiguration.BucketName,
                    playList.LargePictureKeyName), cancellationToken);

            database.PlayLists.Remove(playList);
            await database.SaveChangesAsync(cancellationToken);
            return Result.NoContent();
        }
    }
}