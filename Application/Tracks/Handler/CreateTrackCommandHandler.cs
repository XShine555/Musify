using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Handler
{
    public class CreateTrackCommandHandler(IDatabase database, IEventBus eventBus, IStorageHandler storageHandler,
        ILogger<CreateTrackCommandHandler> logger, ApplicationStorageConfiguration storageConfiguration, TrackConfiguration trackConfiguration)
        : IRequestHandler<CreateTrackCommand, Task<Result<TrackResponse> >>
    {
        public async Task<Result<TrackResponse>> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.FindAsync(request.UserId, cancellationToken);
            if (userExists is null)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var trackEntity = CreateTrackCommand.ToEntity(
                request,
                trackConfiguration.Routes.PresetSmallPicture,
                trackConfiguration.Routes.PresetMediumPicture,
                trackConfiguration.Routes.PresetLargePicture);

            var fullPictureKey = Path.Combine(trackConfiguration.Routes.OriginalPicturesPath, trackEntity.OriginalPictureName);
            var fullAudioKey = Path.Combine(trackConfiguration.Routes.OriginalAudiosPath, trackEntity.OriginalAudioName);

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
            var pictureUpload = await storageHandler.UploadFileAsync(
                request.Picture.FileStream,
                request.Picture.ContentType,
                storageConfiguration.BucketName,
                pictureKey,
                cancellationToken);

            if (!pictureUpload.IsSuccess)
            {
                var errors = string.Join(", ", pictureUpload.Errors);
                return Result.Error($"Failed to upload picture for {request.Title}");
            }

            var audioUpload = await storageHandler.UploadFileAsync(
                request.Audio.FileStream,
                request.Audio.ContentType,
                storageConfiguration.BucketName,
                audioKey,
                cancellationToken);

            if (!audioUpload.IsSuccess)
            {
                await RollbackFilesAsync(pictureKey, audioKey, cancellationToken);
                var errors = string.Join(", ", audioUpload.Errors);
                return Result.Error($"Failed to upload audio for {request.Title}");
            }

            return Result.Success();
        }

        async Task RollbackFilesAsync(string pictureKey, string audioKey, CancellationToken cancellationToken)
        {
            await storageHandler.RemoveFileAsync(storageConfiguration.BucketName, pictureKey, cancellationToken);
            await storageHandler.RemoveFileAsync(storageConfiguration.BucketName, audioKey, cancellationToken);
            logger.LogInformation("Rolled back uploaded files {PictureKey}, {AudioKey}", pictureKey, audioKey);
        }

        async Task PublishUpdateEvent(Track track, CancellationToken cancellationToken)
        {
            try
            {
                await eventBus.PublishAsync(new UpdateTrackPictureEvent(
                    track.Id,
                    storageConfiguration.BucketName,
                    track.OriginalPictureName,
                    new ImageSize(
                        trackConfiguration.Routes.PresetSmallPicture,
                        trackConfiguration.PicturesSizes.SmallPictureWidth,
                        trackConfiguration.PicturesSizes.SmallPictureHeight),
                    new ImageSize(
                        trackConfiguration.Routes.PresetMediumPicture,
                        trackConfiguration.PicturesSizes.MediumPictureWidth,
                        trackConfiguration.PicturesSizes.MediumPictureHeight),
                    new ImageSize(
                        trackConfiguration.Routes.PresetLargePicture,
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
            var destinationFolderAudio = Path.Combine(trackConfiguration.Routes.ProcessedAudiosPath,
                Guid.NewGuid().ToString());

            try
            {
                await eventBus.PublishAsync(new UpdateTrackAudioEvent(
                    track.Id,
                    storageConfiguration.BucketName,
                    track.OriginalAudioName,
                    storageConfiguration.BucketName,
                    destinationFolderAudio), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish track audio update event for track {TrackId}", track.Id);
            }
        }
    }
}