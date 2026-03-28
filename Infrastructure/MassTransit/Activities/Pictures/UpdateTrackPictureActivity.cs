using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Logs;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class UpdateTrackPictureActivity(
        IDatabase database,
        ILogger<UpdateTrackPictureActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<UpdateTrackPictureArguments, UpdateTrackPictureLog>
    {
        public const string ExecuteEndpointName = "update-track-picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdateTrackPictureArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(UpdateTrackPictureActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

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
                    logger.LogWarning("Track with id={TrackId} not found",
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

                database.Tracks.Update(track);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated Track={TrackId} pictures",
                    executeContext.Arguments.TrackId);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error updating Track={TrackId} pictures",
                    executeContext.Arguments.TrackId);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
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
                {
                    return compensateContext.Compensated();
                }

                track.OriginalPictureName = compensateContext.Log.PreviousOriginalPictureKey;
                track.SmallPictureName = compensateContext.Log.PreviousSmallPictureKey;
                track.MediumPictureName = compensateContext.Log.PreviousMediumPictureKey;
                track.LargePictureName = compensateContext.Log.PreviousLargePictureKey;

                database.Tracks.Update(track);
                await database.SaveChangesAsync(compensateContext.CancellationToken);

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error compensating Track={TrackId} pictures", compensateContext.Log.TrackId);
                return compensateContext.Failed(exception);
            }
        }
    }
}