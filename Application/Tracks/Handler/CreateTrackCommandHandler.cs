using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Events;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Responses;
using Musify.Application.UploadIntents;
using Musify.Application.Extensions;
using Musify.Application.Abstractions.Application;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Handler
{
    public class CreateTrackCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        IUploadIntentService uploadIntentService,
        IStorageService storageService,
        ILogger<CreateTrackCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<CreateTrackCommand, Result<TrackApplicationResponse>>
    {
        public async ValueTask<Result<TrackApplicationResponse>> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var pictureValidation = await uploadIntentService.ValidateAndLoadAsync(
                uploadIntentConfiguration,
                request.PictureIntentId, request.UserId, cancellationToken);
            if (!pictureValidation.IsSuccess)
                return pictureValidation.As<UploadIntent, TrackApplicationResponse>();

            var audioValidation = await uploadIntentService.ValidateAndLoadAsync(
                uploadIntentConfiguration,
                request.AudioIntentId, request.UserId, cancellationToken);
            if (!audioValidation.IsSuccess)
                return audioValidation.As<UploadIntent, TrackApplicationResponse>();

            var pictureIntent = pictureValidation.Value;
            var audioIntent = audioValidation.Value;

            var finalPictureKey = trackConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureIntent.ObjectName);
            var finalAudioKey = trackConfiguration.Routes.BuildOriginalAudioPath(request.UserId, audioIntent.ObjectName);

            try
            {
                await storageService.CopyFileAsync(pictureIntent.Bucket, pictureIntent.Key, pictureIntent.Bucket, finalPictureKey, cancellationToken);
                await storageService.CopyFileAsync(audioIntent.Bucket, audioIntent.Key, audioIntent.Bucket, finalAudioKey, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to copy track files from temp to final location for user {UserId}", request.UserId);
                return Result.Error("Failed to move uploaded files to their final locations.");
            }

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

            pictureIntent.Status = UploadIntentStatus.Consumed;
            audioIntent.Status = UploadIntentStatus.Consumed;

            var publishPictureResult = await PublishUpdateEvent(request.UserId, trackEntity, cancellationToken);
            if (!publishPictureResult.IsSuccess)
                return Result.Error($"Failed to create track for {request.Title}");

            var publishTranscodeResult = await PublishTranscodeEvent(request.UserId, trackEntity, cancellationToken);
            if (!publishTranscodeResult.IsSuccess)
                return Result.Error($"Failed to create track for {request.Title}");

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to save track {Title}", request.Title);
                return Result.Error($"Failed to create track for {request.Title}");
            }

            return Result.Created(TrackApplicationResponse.FromEntity(trackEntity));
        }

        async Task<Result> PublishUpdateEvent(Guid userId, Track track, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdateTrackPictureEvent(
                    track.Id,
                    storageConfiguration.Bucket,
                    trackConfiguration.Routes.BuildOriginalPicturePath(userId, track.OriginalPictureName),
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
                        trackConfiguration.PicturesSizes.LargePictureHeight)), cancellationToken);
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish track picture update event for track {TrackId}", track.Id);
                return Result.Error($"Failed to publish track picture update event for track {track.Id}");
            }
        }

        async Task<Result> PublishTranscodeEvent(Guid userId, Track track, CancellationToken cancellationToken)
        {
            var destinationFolderAudio = trackConfiguration.Routes.BuildProcessedAudioPath(Guid.NewGuid().ToString());

            try
            {
                await eventBus.PublishAsync(new UpdateTrackAudioEvent(
                    track.Id,
                    storageConfiguration.Bucket,
                    trackConfiguration.Routes.BuildOriginalAudioPath(userId, track.OriginalAudioName),
                    storageConfiguration.Bucket,
                    destinationFolderAudio), cancellationToken);
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish track audio update event for track {TrackId}", track.Id);
                return Result.Error($"Failed to publish track audio update event for track {track.Id}");
            }
        }
    }
}
