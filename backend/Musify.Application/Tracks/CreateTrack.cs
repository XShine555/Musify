using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Shared;
using Musify.Application.Events;
using Musify.Application.Tracks.Responses;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks
{
    public record CreateTrackCommand(
        long UserId,
        string Title,
        Guid PictureIntentId,
        Guid AudioIntentId)
        : ICommand<ErrorOr<TrackApplicationResponse>>;

    public class CreateTrackCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        UploadIntentValidator uploadIntentValidator,
        ILogger<CreateTrackCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<CreateTrackCommand, ErrorOr<TrackApplicationResponse>>
    {
        public async ValueTask<ErrorOr<TrackApplicationResponse>> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Error.NotFound(description: $"User {request.UserId} not found");
            }

            var pictureValidation = await uploadIntentValidator.ValidateAndLoadAsync(
                uploadIntentConfiguration,
                request.PictureIntentId, request.UserId, cancellationToken);
            if (pictureValidation.IsError)
                return pictureValidation.Errors;

            var audioValidation = await uploadIntentValidator.ValidateAndLoadAsync(
                uploadIntentConfiguration,
                request.AudioIntentId, request.UserId, cancellationToken);
            if (audioValidation.IsError)
                return audioValidation.Errors;

            var pictureIntent = pictureValidation.Value;
            var audioIntent = audioValidation.Value;

            var finalPictureKey = trackConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureIntent.ObjectName);
            var finalAudioKey = trackConfiguration.Routes.BuildOriginalAudioPath(request.UserId, audioIntent.ObjectName);
            var audioProcessedFolderKey = trackConfiguration.Routes.BuildProcessedAudioPath(Guid.NewGuid().ToString());

            var trackEntity = new Track
            {
                Title = request.Title,
                NormalizedTitle = request.Title.ToUpperInvariant(),
                OriginalPictureName = pictureIntent.ObjectName,
                OriginalAudioName = audioIntent.ObjectName,
                SmallPictureName = trackConfiguration.Routes.PresetSmallPicture,
                MediumPictureName = trackConfiguration.Routes.PresetMediumPicture,
                LargePictureName = trackConfiguration.Routes.PresetLargePicture,
                PicturesProcessingStatus = ProcessingStatus.Pending,
                AudioTranscodeProcessingStatus = ProcessingStatus.Pending
            };

            await database.Tracks.AddAsync(trackEntity, cancellationToken);

            await database.UserHasTracks.AddAsync(new UserHasTrack
            {
                UserId = request.UserId,
                TrackId = trackEntity.Id
            }, cancellationToken);

            try
            {
                await eventBus.PublishAsync(
                    new CreateTrackResourcesEvent(
                        trackEntity.Id,
                        pictureIntent.Id,
                        audioIntent.Id,
                        storageConfiguration.Bucket,
                        pictureIntent.Key,
                        finalPictureKey,
                        audioIntent.Key,
                        finalAudioKey,
                        audioProcessedFolderKey,
                        new ImageSize(
                            trackConfiguration.Routes.SmallPicturesPath,
                            trackConfiguration.PicturesSizes.SmallPictureWidth,
                            trackConfiguration.PicturesSizes.SmallPictureHeight),
                        new ImageSize(
                            trackConfiguration.Routes.MediumPicturesPath,
                            trackConfiguration.PicturesSizes.MediumPictureWidth,
                            trackConfiguration.PicturesSizes.MediumPictureHeight),
                        new ImageSize(
                            trackConfiguration.Routes.LargePicturesPath,
                            trackConfiguration.PicturesSizes.LargePictureWidth,
                            trackConfiguration.PicturesSizes.LargePictureHeight)),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish create track event for track {TrackId}", trackEntity.Id);
                return Error.Failure(description: $"Failed to create track for {request.Title}");
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save track {Title}", request.Title);
                return Error.Failure(description: $"Failed to create track for {request.Title}");
            }

            return TrackApplicationResponse.FromEntity(trackEntity, listensCount: 0);
        }
    }
}
