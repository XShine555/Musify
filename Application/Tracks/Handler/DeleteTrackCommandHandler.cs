using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
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
            var track = await database.Tracks.SingleOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);
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
            await RemoveFile(trackConfiguration.Routes.BuildOriginalPicturePath(track.OriginalPictureName),
                cancellationToken);

            if (track.SmallPictureName != trackConfiguration.Routes.PresetSmallPicture)
                await RemoveFile(trackConfiguration.Routes.BuildSmallPicturePath(track.SmallPictureName),
                    cancellationToken);
            if (track.MediumPictureName != trackConfiguration.Routes.PresetMediumPicture)
                await RemoveFile(trackConfiguration.Routes.BuildMediumPicturePath(track.MediumPictureName),
                    cancellationToken);
            if (track.LargePictureName != trackConfiguration.Routes.PresetLargePicture)
                await RemoveFile(trackConfiguration.Routes.BuildLargePicturePath(track.LargePictureName),
                    cancellationToken);

        }

        async Task RemoveAudios(Track track, CancellationToken cancellationToken)
        {
            await RemoveFile(trackConfiguration.Routes.BuildOriginalAudioPath(track.OriginalAudioName),
                cancellationToken);
            await RemoveFile(trackConfiguration.Routes.BuildProcessedAudioPath(track.AudioFolderName),
                cancellationToken);
        }

        async Task RemoveFile(string path, CancellationToken cancellationToken)
        {
            try
            {
                await storageHandler.RemoveFileAsync(storageConfiguration.Bucket, path, cancellationToken);
                logger.LogDebug("Removed file {Path} from storage", path);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to remove file {Path} from storage", path);
            }
        }
    }
}