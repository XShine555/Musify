using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Handlers
{
    public class CreatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        ILogger<CreatePlayListCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
        : ICommandHandler<CreatePlayListCommand, Result<PlayListApplicationResponse>>
    {
        public async ValueTask<Result<PlayListApplicationResponse>> Handle(CreatePlayListCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var originalPictureName = request.OriginalPictureName ?? playListConfiguration.Routes.PresetOriginalPicture;

            var playList = new PlayList
            {
                UserId = request.UserId,
                Name = request.Name,
                NormalizedName = request.Name.Trim().ToUpperInvariant(),
                Description = request.Description,
                OriginalPictureName = originalPictureName,
                SmallPictureName = playListConfiguration.Routes.PresetSmallPicture,
                MediumPictureName = playListConfiguration.Routes.PresetMediumPicture,
                LargePictureName = playListConfiguration.Routes.PresetLargePicture
            };

            await database.PlayLists.AddAsync(playList, cancellationToken);

            if (request.OriginalPictureName is not null)
            {
                var publishResult = await PublishPictureUpdateEventAsync(playList, cancellationToken);
                if (!publishResult.IsSuccess)
                    return publishResult;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save playlist for user {UserId}", request.UserId);
                return Result.Error($"Failed to create playlist for user {request.UserId}");
            }

            logger.LogInformation("Created playlist {PlayListId} for user {UserId}", playList.Id, request.UserId);
            return Result.Created(PlayListApplicationResponse.FromEntity(playList));
        }

        async Task<Result> PublishPictureUpdateEventAsync(PlayList playList, CancellationToken cancellationToken)
        {
            try
            {
                var updateEvent = new UpdatePlayListPictureEvent(
                    playList.Id,
                    storageConfiguration.Bucket,
                    playListConfiguration.Routes.BuildOriginalPicturePath(playList.UserId, playList.OriginalPictureName),
                    new ImageSize(
                        playListConfiguration.Routes.SmallPicturesPath,
                        playListConfiguration.PicturesSizes.SmallPictureWidth,
                        playListConfiguration.PicturesSizes.SmallPictureHeight),
                    new ImageSize(
                        playListConfiguration.Routes.MediumPicturesPath,
                        playListConfiguration.PicturesSizes.MediumPictureWidth,
                        playListConfiguration.PicturesSizes.MediumPictureHeight),
                    new ImageSize(
                        playListConfiguration.Routes.LargePicturesPath,
                        playListConfiguration.PicturesSizes.LargePictureWidth,
                        playListConfiguration.PicturesSizes.LargePictureHeight));

                await eventBus.PublishAsync(updateEvent, cancellationToken);
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish picture update event for playlist {PlayListId}", playList.Id);
                return Result.Error($"Failed to publish picture update event for playlist {playList.Id}");
            }
        }
    }
}