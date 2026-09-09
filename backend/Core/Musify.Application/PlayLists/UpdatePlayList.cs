using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Shared;
using Musify.Application.Events;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists
{
    public record UpdatePlayListCommand(
        long UserId,
        Guid PlayListId,
        string? NewName,
        string? NewDescription,
        Guid? NewPictureIntentId)
        : ICommand<ErrorOr<PlayListApplicationResponse>>;

    public class UpdatePlayListCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<UpdatePlayListCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<UpdatePlayListCommand, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
        {
            var playListEntity = await database.PlayLists.SingleOrDefaultAsync(pl => pl.Id == request.PlayListId, cancellationToken);
            if (playListEntity is null)
            {
                logger.LogInformation("Playlist {PlayListId} not found", request.PlayListId);
                return Error.NotFound();
            }

            if (playListEntity.UserId != request.UserId)
            {
                logger.LogWarning("User {UserId} is not the owner of playlist {PlayListId}", request.UserId, request.PlayListId);
                return Error.Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(request.NewName))
            {
                playListEntity.Name = request.NewName;
                playListEntity.NormalizedName = request.NewName.Trim().ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(request.NewDescription))
            {
                playListEntity.Description = request.NewDescription;
            }

            UploadIntent? pictureIntent = null;
            string? finalPictureKey = null;

            if (request.NewPictureIntentId.HasValue)
            {
                var validation = await uploadIntentValidator.ValidateAndLoadAsync(
                    uploadIntentConfiguration,
                    request.NewPictureIntentId.Value, request.UserId, cancellationToken);
                if (validation.IsError)
                    return validation.Errors;

                pictureIntent = validation.Value;
                playListEntity.Pictures = new PlayListPictures 
                { 
                    OriginalName = pictureIntent.ObjectName,
                    SmallName = playListConfiguration.Routes.PresetSmallPicture,
                    MediumName = playListConfiguration.Routes.PresetMediumPicture,
                    LargeName = playListConfiguration.Routes.PresetLargePicture
                };
                finalPictureKey = playListConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureIntent.ObjectName);
            }

            database.PlayLists.Update(playListEntity);

            if (pictureIntent != null&& finalPictureKey != null)
            {
                var publishResult = await PublishUpdatePlayListPictureSourceEventAsync(
                    playListEntity.Id, pictureIntent, finalPictureKey, cancellationToken);
                if (publishResult.IsError)
                    return publishResult.Errors;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update playlist {PlayListId}", playListEntity.Id);
                return Error.Failure(description: $"Failed to update playlist {playListEntity.Id}");
            }

            var coverTrackIds = await database.PlayListHasTracks
                .AsNoTracking()
                .Where(plt => plt.PlayListId == playListEntity.Id)
                .OrderBy(plt => plt.Position)
                .Take(PlayListApplicationResponse.CoverTrackCount)
                .Select(plt => plt.TrackId)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Updated playlist {PlayListId}", playListEntity.Id);
            return PlayListApplicationResponse.FromEntity(playListEntity, coverTrackIds);
        }

        async Task<ErrorOr<Success>> PublishUpdatePlayListPictureSourceEventAsync(
            Guid playListId,
            UploadIntent pictureIntent,
            string finalPictureKey,
            CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(
                    new UpdatePlayListPictureSourceEvent(
                        playListId,
                        pictureIntent.Id,
                        storageConfiguration.Bucket,
                        pictureIntent.Key,
                        finalPictureKey,
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
                            playListConfiguration.PicturesSizes.LargePictureHeight)),
                    cancellationToken);
                return new Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish update playlist picture source event for playlist {PlayListId}", playListId);
                return Error.Failure(description: $"Failed to publish update playlist picture source event for playlist {playListId}");
            }
        }
    }
}
