using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Application.Tracks.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Commands.CreateTrack
{
    public class CreateTrackCommandHandler(IDatabase database, IPictureHandler pictureHandler, IEventBus eventBus, IStorageHandler storageHandler,
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

            var originalPictureKeyName = $"{Guid.NewGuid() }.{request.Picture.FileType}";
            var track = new Track
            {
                Title = request.Title,
                NormalizedTitle = request.Title.Trim().ToUpperInvariant(),
                SmallPictureKeyName = trackConfiguration.Routes.PresetSmallPicture,
                MediumPictureKeyName = trackConfiguration.Routes.PresetMediumPicture,
                LargePictureKeyName = trackConfiguration.Routes.PresetLargePicture,
            };

            var pictureUploadResult = await storageHandler.UploadFileAsync(
                request.Picture.FileStream,
                request.Picture.ContentType,
                storageConfiguration.BucketName,
                originalPictureKeyName,
                cancellationToken);

            if (!pictureUploadResult.IsSuccess)
            {
                logger.LogError("Failed to upload picture for track {TrackTitle}", request.Title);
                return Result.Error($"Failed to upload picture for track {request.Title}");
            }

            var smallPictureKeyName = Path.Combine(
                trackConfiguration.Routes.SmallPictures,
                $"{track.Id}.{pictureHandler.FileExtension}");
            var mediumPictureKeyName = Path.Combine(
                trackConfiguration.Routes.MediumPictures,
                $"{track.Id}.{pictureHandler.FileExtension}");
            var largePictureKeyName = Path.Combine(
                trackConfiguration.Routes.LargePictures,
                $"{track.Id}.{pictureHandler.FileExtension}");

            try
            {
                await eventBus.PublishAsync(new UpdatePlayListPictureEvent(
                    track.Id,
                    storageConfiguration.BucketName,
                    originalPictureKeyName,
                    smallPictureKeyName,
                    trackConfiguration.PicturesSizes.SmallPictureWidth,
                    trackConfiguration.PicturesSizes.SmallPictureHeight,
                    mediumPictureKeyName,
                    trackConfiguration.PicturesSizes.MediumPictureWidth,
                    trackConfiguration.PicturesSizes.MediumPictureHeight,
                    largePictureKeyName,
                    trackConfiguration.PicturesSizes.LargePictureWidth,
                    trackConfiguration.PicturesSizes.LargePictureHeight), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to publish resize picture event for Track with id={TrackId}.", track.Id);
                return Result.Error($"Failed to publish resize picture event for Track with id {track.Id}.");
            }

            await database.Tracks.AddAsync(track, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
