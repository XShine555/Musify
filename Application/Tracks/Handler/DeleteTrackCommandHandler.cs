using Ardalis.Result;
using Mediator;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Tracks.Commands;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Handler
{
    public class DeleteTrackCommandHandler(IDatabase database, IStorageService storageHandler, ILogger<DeleteTrackCommandHandler> logger,
        TrackConfiguration trackConfiguration, ApplicationStorageConfiguration storageConfiguration)
        : ICommandHandler<DeleteTrackCommand, Result>
    {
        public async ValueTask<Result> Handle(DeleteTrackCommand request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.FindAsync(request.TrackId, cancellationToken);
            if (track is null)
            {
                logger.LogInformation("Track {TrackId} not found", request.TrackId);
                return Result.NotFound();
            }

            if (track.Id != request.UserId)
            {
                logger.LogWarning("User {UserId} unauthorized to delete track {TrackId}", request.UserId, request.TrackId);
                return Result.Unauthorized();
            }

            await RemovePictures(track, cancellationToken);
            await RemoveAudios(track, cancellationToken);

            return Result.NoContent();
        }

        async Task RemovePictures(Track track, CancellationToken cancellationToken)
        {
            await RemoveFile(Path.Combine(trackConfiguration.Routes.OriginalPicturesPath, track.OriginalPictureName),
                cancellationToken);

            if (track.SmallPictureName != trackConfiguration.Routes.PresetSmallPicture)
                await RemoveFile(Path.Combine(trackConfiguration.Routes.SmallPicturesPath, track.SmallPictureName),
                    cancellationToken);
            if (track.MediumPictureName != trackConfiguration.Routes.PresetMediumPicture)
                await RemoveFile(Path.Combine(trackConfiguration.Routes.MediumPicturesPath, track.MediumPictureName),
                    cancellationToken);
            if (track.LargePictureName != trackConfiguration.Routes.PresetLargePicture)
                await RemoveFile(Path.Combine(trackConfiguration.Routes.LargePicturesPath, track.LargePictureName),
                    cancellationToken);

        }

        async Task RemoveAudios(Track track, CancellationToken cancellationToken)
        {
            await RemoveFile(Path.Combine(trackConfiguration.Routes.OriginalAudiosPath, track.OriginalAudioName),
                cancellationToken);
            await RemoveFile(Path.Combine(trackConfiguration.Routes.ProcessedAudiosPath, track.AudioFolderName),
                cancellationToken);
        }

        async Task RemoveFile(string path, CancellationToken cancellationToken)
        {
            await storageHandler.RemoveFileAsync(storageConfiguration.Bucket, path, cancellationToken);
            logger.LogDebug("Removed file {Path} from storage", path);
        }
    }
}