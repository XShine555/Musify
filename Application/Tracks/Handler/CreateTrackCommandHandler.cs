using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Application;
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
                logger.LogWarning("User with id {UserId} not found", request.UserId);
                return Result.NotFound($"User with id {request.UserId} not found");
            }

            var originalPictureFileName = Guid.NewGuid() + request.Picture.FileType;
            var originalAudioFileName = Guid.NewGuid() + request.Audio.FileType;
            var track = new Track
            {
                Title = request.Title,
                NormalizedTitle = request.Title.Trim().ToUpperInvariant(),
                OriginalPictureKeyName = originalPictureFileName,
                OriginalAudioKeyName = originalAudioFileName,
                SmallPictureKeyName = trackConfiguration.Routes.PresetMediumPicture,
                MediumPictureKeyName = trackConfiguration.Routes.PresetMediumPicture,
                LargePictureKeyName = trackConfiguration.Routes.PresetLargePicture,
            };

            var originalPictureKeyName = Path.Combine(trackConfiguration.Routes.OriginalPicturesPath,
                originalPictureFileName);
            var pictureUploadResult = await UploadFile(request.Picture, originalPictureKeyName, cancellationToken);
            if (!pictureUploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for track {TrackTitle}", request.Title);
                return Result.Error($"Failed to upload picture for track {request.Title}");
            }


            var audioKeyName = Path.Combine(trackConfiguration.Routes.OriginalAudiosPath,
                Guid.NewGuid() + request.Audio.FileType);
            var audioUploadResult = await UploadFile(request.Audio, audioKeyName, cancellationToken);
            if (!audioUploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload audio for track {TrackTitle}", request.Title);
                return Result.Error($"Failed to upload audio for track {request.Title}");
            }

            try
            {
                await PublishUpdateEvent(originalPictureKeyName, track, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish resize picture event for Track with id={TrackId}.", track.Id);
                return Result.Error($"Failed to publish resize picture event for Track with id {track.Id}.");
            }

            try
            {
                await PublishTranscodeEvent(track.Id, audioUploadResult, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish transcode audio event for Track with id={TrackId}.", track.Id);
                return Result.Error($"An error occurred while processing the audio for Track with id {track.Id}.");
            }

            await database.Tracks.AddAsync(track, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        async Task<Result<string>> UploadFile(IFileData fileData, string keyName, CancellationToken cancellationToken)
        {
            var uploadFile = await storageHandler.UploadFileAsync(
                fileData.FileStream,
                fileData.ContentType,
                storageConfiguration.BucketName,
                keyName,
                cancellationToken);

            return uploadFile;
        }

        async Task PublishUpdateEvent(string originalPictureKeyName, Track track, CancellationToken cancellationToken)
        {
            await eventBus.PublishAsync(new UpdateTrackPictureEvent(
                track.Id,
                storageConfiguration.BucketName,
                originalPictureKeyName,
                trackConfiguration.Routes.SmallPicturesPath,
                trackConfiguration.PicturesSizes.SmallPictureWidth,
                trackConfiguration.PicturesSizes.SmallPictureHeight,
                trackConfiguration.Routes.MediumPicturesPath,
                trackConfiguration.PicturesSizes.MediumPictureWidth,
                trackConfiguration.PicturesSizes.MediumPictureHeight,
                trackConfiguration.Routes.LargePicturesPath,
                trackConfiguration.PicturesSizes.LargePictureWidth,
                trackConfiguration.PicturesSizes.LargePictureHeight), cancellationToken);
        }

        async Task PublishTranscodeEvent(Guid trackId, string audioKeyName, CancellationToken cancellationToken)
        {
            var destinationFolderAudio = Path.Combine(trackConfiguration.Routes.ProcessedAudiosPath,
                Guid.NewGuid().ToString());

            await eventBus.PublishAsync(new UpdateTrackAudioEvent(
                trackId,
                storageConfiguration.BucketName,
                audioKeyName,
                storageConfiguration.BucketName,
                destinationFolderAudio), cancellationToken);
        }
    }
}
