using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Handler
{
    public class CreateTrackCommandHandler(IDatabase database, IEventBus eventBus, IStorageService storageHandler,
        ILogger<CreateTrackCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration, TrackConfiguration trackConfiguration)
        : ICommandHandler<CreateTrackCommand, Result<TrackResponse> >
    {
        public async ValueTask<Result<TrackResponse>> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var trackEntity = CreateTrackCommand.ToEntity(
                request,
                trackConfiguration.Routes.PresetSmallPicture,
                trackConfiguration.Routes.PresetMediumPicture,
                trackConfiguration.Routes.PresetLargePicture);

            var fullPictureKey = trackConfiguration.Routes.BuildOriginalPicturePath(trackEntity.OriginalPictureName);
            var fullAudioKey = trackConfiguration.Routes.BuildOriginalAudioPath(trackEntity.OriginalAudioName);

            await UploadFilesAsync(request, fullPictureKey, fullAudioKey, cancellationToken);

            await PublishUpdateEvent(trackEntity, cancellationToken);
            await PublishTranscodeEvent(trackEntity, cancellationToken);

            await database.Tracks.AddAsync(trackEntity, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        async Task<Result> UploadFilesAsync(
            CreateTrackCommand request,
            string pictureKey,
            string audioKey,
            CancellationToken cancellationToken)
        {
            try
            {
                await storageHandler.UploadFileAsync(
                    request.Picture.FileStream,
                    request.Picture.ContentType,
                    storageConfiguration.Bucket,
                    pictureKey,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload picture for track {Title}", request.Title);
                return Result.Error($"Failed to upload picture for {request.Title}");
            }

            try
            {
                await storageHandler.UploadFileAsync(
                    request.Audio.FileStream,
                    request.Audio.ContentType,
                    storageConfiguration.Bucket,
                    audioKey,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload audio for track {Title}", request.Title);
                await RollbackFilesAsync(pictureKey, audioKey, cancellationToken);
                return Result.Error($"Failed to upload audio for {request.Title}");
            }
            return Result.Success();
        }

        async Task RollbackFilesAsync(string pictureKey, string audioKey, CancellationToken cancellationToken)
        {
            await storageHandler.RemoveFileAsync(storageConfiguration.Bucket, pictureKey, cancellationToken);
            await storageHandler.RemoveFileAsync(storageConfiguration.Bucket, audioKey, cancellationToken);
            logger.LogInformation("Rolled back uploaded files {PictureKey}, {AudioKey}", pictureKey, audioKey);
        }

        async Task PublishUpdateEvent(Track track, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdateTrackPictureEvent(
                    track.Id,
                    storageConfiguration.Bucket,
                    trackConfiguration.Routes.BuildOriginalPicturePath(track.OriginalPictureName),
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
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish track picture update event for track {TrackId}", track.Id);
            }
        }

        async Task PublishTranscodeEvent(Track track, CancellationToken cancellationToken)
        {
            var destinationFolderAudio = trackConfiguration.Routes.BuildProcessedAudioPath(Guid.NewGuid().ToString());

            try
            {
                await eventBus.PublishAsync(new UpdateTrackAudioEvent(
                    track.Id,
                    storageConfiguration.Bucket,
                    trackConfiguration.Routes.BuildOriginalAudioPath(track.OriginalAudioName),
                    storageConfiguration.Bucket,
                    destinationFolderAudio), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish track audio update event for track {TrackId}", track.Id);
            }
        }
    }
}