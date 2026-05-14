using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Domain.Entities;

namespace Musify.Infrastructure.MassTransit.Activities.Tracks
{
    internal class DeleteTrackActivity(
        IDatabase database,
        IStorageService storageService,
        TrackConfiguration trackConfiguration,
        ApplicationStorageConfiguration storageConfiguration,
        ILogger<DeleteTrackActivity> logger)
        : IExecuteActivity<DeleteTrackArguments>
    {
        public const string ExecuteEndpointName = "delete-track";

        public async Task<ExecutionResult> Execute(ExecuteContext<DeleteTrackArguments> executeContext)
        {
            Track track;
            try
            {
                var getTrack = await database.Tracks.SingleOrDefaultAsync(t => t.Id == executeContext.Arguments.TrackId, executeContext.CancellationToken);
                track = getTrack ?? throw new InvalidOperationException($"Track with id {executeContext.Arguments.TrackId} not found");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to retrieve track {TrackId} for deletion", executeContext.Arguments.TrackId);
                throw;
            }

            try
            {
                await RemovePicturesAsync(track, executeContext.Arguments.UserId, executeContext.CancellationToken);
                track.PicturesProcessingStatus = ProcessingStatus.Completed;
                await database.SaveChangesAsync(executeContext.CancellationToken);

                await RemoveAudiosAsync(track, executeContext.Arguments.UserId, executeContext.CancellationToken);
                track.AudioTranscodeProcessingStatus = ProcessingStatus.Completed;
                await database.SaveChangesAsync(executeContext.CancellationToken);

                database.Tracks.Remove(track);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Deleted track {TrackId}", track.Id);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to delete track {TrackId}", executeContext.Arguments.TrackId);

                try
                {
                    track.PicturesProcessingStatus = ProcessingStatus.Failed;
                    track.AudioTranscodeProcessingStatus = ProcessingStatus.Failed;
                    await database.SaveChangesAsync(executeContext.CancellationToken);
                }
                catch (Exception dbException)
                {
                    logger.LogError(dbException, "Failed to update processing statuses to Failed for track {TrackId}", executeContext.Arguments.TrackId);
                }

                throw;
            }
        }

        async Task RemovePicturesAsync(Track track, Guid userId, CancellationToken cancellationToken)
        {
            await RemoveFileAsync(trackConfiguration.Routes.BuildOriginalPicturePath(userId, track.OriginalPictureName), cancellationToken);

            if (!track.IsPicturesProcessed)
            {
                logger.LogInformation(
                    "Track {TrackId} pictures are not fully processed, skipping removal of small, medium, and large pictures",
                    track.Id);
                return;
            }

            await RemoveFileAsync(trackConfiguration.Routes.BuildSmallPicturePath(track.SmallPictureName), cancellationToken);
            await RemoveFileAsync(trackConfiguration.Routes.BuildMediumPicturePath(track.MediumPictureName), cancellationToken);
            await RemoveFileAsync(trackConfiguration.Routes.BuildLargePicturePath(track.LargePictureName), cancellationToken);
        }

        async Task RemoveAudiosAsync(Track track, Guid userId, CancellationToken cancellationToken)
        {
            await RemoveFileAsync(trackConfiguration.Routes.BuildOriginalAudioPath(userId, track.OriginalAudioName), cancellationToken);

            if (!track.IsAudioProcessed)
            {
                logger.LogInformation(
                    "Track {TrackId} audio is not fully processed, skipping removal of processed audio",
                    track.Id);
                return;
            }

            await RemoveFileAsync(trackConfiguration.Routes.BuildProcessedAudioPath(track.AudioFolderName), cancellationToken);
        }

        async Task RemoveFileAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogDebug("Removing file {Key} from bucket {Bucket}", key, storageConfiguration.Bucket);
                await storageService.RemoveFileAsync(storageConfiguration.Bucket, key, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to remove file {Key} from bucket {Bucket}", key, storageConfiguration.Bucket);
                throw;
            }
        }
    }
}
