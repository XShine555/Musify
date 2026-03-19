using Ardalis.Result;
using DispatchR.Abstractions.Send;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Commands.UpdatePlayList
{
    public class UpdatePlayListCommandHandler(IPublishEndpoint publishEndpoint, IDatabase database, IStorageHandler storageHandler,
        ILogger<UpdatePlayListCommandHandler> logger, StorageSettings storageConfiguration, PlayListConfiguration playListConfiguration)
        : IRequestHandler<UpdatePlayListCommand, Task<Result<PlayListResponse>> >
    {
        public async Task<Result<PlayListResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists.SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);

            if (playList is null)
            {
                logger.LogInformation("PlayList with id {PlayListId} not found", request.PlayListId);
                return Result.NotFound();
            }

            if (playList.UserId != request.UserId)
            {
                logger.LogWarning("User with id {UserId} is not the owner of the PlayList with id {PlayListId}", request.UserId, request.PlayListId);
                return Result.Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(request.NewName))
                playList.Name = request.NewName;

            if (!string.IsNullOrWhiteSpace(request.NewDescription))
                playList.Description = request.NewDescription;

            if (request.NewPicture is null)
            {
                database.PlayLists.Update(playList);
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Updated PlayList with id={PlayListId} without picture changes.", playList.Id);
                return Result.Success(PlayListResponse.FromEntity(playList));
            }

            if (!string.IsNullOrWhiteSpace(playList.OriginalPictureKeyName))
            {
                await publishEndpoint.Publish(new RemoveFileEvent(
                   storageConfiguration.BucketName,
                   playList.OriginalPictureKeyName), cancellationToken);
                logger.LogInformation("Published event to remove old picture with key name {KeyName} from bucket {BucketName}",
                    playList.OriginalPictureKeyName, storageConfiguration.BucketName);

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
            }

            var newImageId = Guid.NewGuid();
            var imageKeyName = $"{playListConfiguration.Routes.OriginalPictures}/{newImageId}{request.NewPicture.FileType}";

            var uploadResult = await storageHandler.UploadFileAsync(
                request.NewPicture.FileStream,
                request.NewPicture.ContentType,
                storageConfiguration.BucketName,
                imageKeyName,
                cancellationToken);

            if (!uploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for PlayList with Id={PlayListId} to storage. Storage handler returned error: {ErrorMessage}",
                    playList.Id, string.Join("; ", uploadResult.Errors));
                return Result.Error($"Failed to upload picture for PlayList with Id {playList.Id} to storage.");
            }

            playList.OriginalPictureKeyName = imageKeyName;
            await publishEndpoint.Publish(new ResizePictureEvent(
                storageConfiguration.BucketName,
                imageKeyName,
                [
                    new ResizePictureItems(
                        playListConfiguration.PicturesSizes.SmallPictureWidth,
                        playListConfiguration.PicturesSizes.SmallPictureHeight,
                        playListConfiguration.Routes.SmallPictures),
                    new ResizePictureItems(
                        playListConfiguration.PicturesSizes.MediumPictureWidth,
                        playListConfiguration.PicturesSizes.MediumPictureHeight,
                        playListConfiguration.Routes.MediumPictures),
                    new ResizePictureItems(
                        playListConfiguration.PicturesSizes.LargePictureWidth,
                        playListConfiguration.PicturesSizes.LargePictureHeight,
                        playListConfiguration.Routes.LargePictures)
                    ]
                ), cancellationToken);

            database.PlayLists.Update(playList);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated PlayList with id={PlayListId} and picture key {PictureKeyName}.", playList.Id, imageKeyName);
            return Result.Success(PlayListResponse.FromEntity(playList));
        }
    }
}