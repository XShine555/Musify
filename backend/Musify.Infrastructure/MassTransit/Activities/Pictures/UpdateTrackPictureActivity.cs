using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Pictures
{
    internal class UpdateTrackPictureActivity(
        IDatabase database,
        ILogger<UpdateTrackPictureActivity> logger)
        : IActivity<UpdateTrackPictureArguments, UpdateTrackPictureLog>
    {
        public const string ExecuteEndpointName = "update-track-picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdateTrackPictureArguments> executeContext)
        {
            var smallResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.SmallPictureVariable);
            ArgumentNullException.ThrowIfNull(smallResizedVariable, nameof(smallResizedVariable));
            var mediumResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.MediumPictureVariable);
            ArgumentNullException.ThrowIfNull(mediumResizedVariable, nameof(mediumResizedVariable));
            var largeResizedVariable = executeContext.GetVariable<string>(executeContext.Arguments.LargePictureVariable);
            ArgumentNullException.ThrowIfNull(largeResizedVariable, nameof(largeResizedVariable));

            try
            {
                var track = await database.Tracks.FindAsync(
                    [executeContext.Arguments.TrackId],
                    cancellationToken: executeContext.CancellationToken);

                if (track is null)
                {
                    logger.LogWarning("Track {TrackId} not found",
                        executeContext.Arguments.TrackId);
                    throw new InvalidOperationException($"Track with id {executeContext.Arguments.TrackId} not found");
                }

                var log = new UpdateTrackPictureLog(
                    track.Id,
                    track.OriginalPictureName,
                    track.SmallPictureName,
                    track.MediumPictureName,
                    track.LargePictureName);

                track.OriginalPictureName = Path.GetFileName(executeContext.Arguments.OriginalPictureKey);
                track.SmallPictureName = Path.GetFileName(smallResizedVariable);
                track.MediumPictureName = Path.GetFileName(mediumResizedVariable);
                track.LargePictureName = Path.GetFileName(largeResizedVariable);
                track.PicturesProcessingStatus = ProcessingStatus.Completed;

                database.Tracks.Update(track);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated track {TrackId} pictures",
                    executeContext.Arguments.TrackId);

                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update track {TrackId} pictures",
                    executeContext.Arguments.TrackId);
                throw;
            }
        }

        public async Task<CompensationResult> Compensate(CompensateContext<UpdateTrackPictureLog> compensateContext)
        {
            try
            {
                var track = await database.Tracks.FindAsync(
                    [compensateContext.Log.TrackId],
                    cancellationToken: compensateContext.CancellationToken);

                if (track is null)
                    return compensateContext.Compensated();

                track.OriginalPictureName = compensateContext.Log.PreviousOriginalPictureKey;
                track.SmallPictureName = compensateContext.Log.PreviousSmallPictureKey;
                track.MediumPictureName = compensateContext.Log.PreviousMediumPictureKey;
                track.LargePictureName = compensateContext.Log.PreviousLargePictureKey;
                track.PicturesProcessingStatus = ProcessingStatus.Failed;

                database.Tracks.Update(track);
                await database.SaveChangesAsync(compensateContext.CancellationToken);

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate track {TrackId} pictures", compensateContext.Log.TrackId);
                return compensateContext.Failed(exception);
            }
        }
    }
}