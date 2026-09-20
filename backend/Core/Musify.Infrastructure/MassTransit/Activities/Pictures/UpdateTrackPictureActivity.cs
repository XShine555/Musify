using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Pictures
{
    internal class UpdateTrackPictureActivity(
        IDatabase database,
        IPublishEndpoint publishEndpoint,
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

                if (track == null)
                {
                    logger.LogWarning("Track {TrackId} not found",
                        executeContext.Arguments.TrackId);
                    throw new InvalidOperationException($"Track with id {executeContext.Arguments.TrackId} not found");
                }

                var log = new UpdateTrackPictureLog(
                    track.Id,
                    track.Pictures.OriginalName,
                    track.Pictures.SmallName,
                    track.Pictures.MediumName,
                    track.Pictures.LargeName);

                track.Pictures.OriginalName = Path.GetFileName(executeContext.Arguments.OriginalPictureKey);
                track.Pictures.SmallName = Path.GetFileName(smallResizedVariable);
                track.Pictures.MediumName = Path.GetFileName(mediumResizedVariable);
                track.Pictures.LargeName = Path.GetFileName(largeResizedVariable);
                track.Pictures.ProcessingStatus = ProcessingStatus.Completed;

                database.Tracks.Update(track);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated track {TrackId} pictures",
                    executeContext.Arguments.TrackId);

                await publishEndpoint.Publish(
                    new TrackPictureProcessed(executeContext.Arguments.TrackId),
                    executeContext.CancellationToken);

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

                if (track == null)
                    return compensateContext.Compensated();

                track.Pictures.OriginalName = compensateContext.Log.PreviousOriginalPictureKey;
                track.Pictures.SmallName = compensateContext.Log.PreviousSmallPictureKey;
                track.Pictures.MediumName = compensateContext.Log.PreviousMediumPictureKey;
                track.Pictures.LargeName = compensateContext.Log.PreviousLargePictureKey;
                track.Pictures.ProcessingStatus = ProcessingStatus.Failed;

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