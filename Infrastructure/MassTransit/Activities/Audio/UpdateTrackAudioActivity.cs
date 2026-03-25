using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;
using Musify.Infrastructure.Messaging.Activities;

namespace Musify.Infrastructure.MassTransit.Activities.Audio
{
    public class UpdateTrackAudioActivity(
        IDatabase database,
        ILogger<TranscodeDashAudioActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<UpdateTrackAudioArguments, UpdateTrackAudioLog>
    {
        public const string ExecuteEndpointName = "update-track-audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdateTrackAudioArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(UpdateTrackAudioActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            try
            {
                var track = await database.Tracks.FindAsync(
                    [executeContext.Arguments.TrackId],
                    cancellationToken: executeContext.CancellationToken);

                if (track is null)
                {
                    logger.LogWarning("Track with id {TrackId} not found",
                        executeContext.Arguments.TrackId);
                    throw new InvalidOperationException($"Track with id {executeContext.Arguments.TrackId} not found");
                }

                var log = new UpdateTrackAudioLog(
                    track.Id,
                    track.AudioFolderKeyName);

                track.AudioFolderKeyName = Path.GetFileName(executeContext.Arguments.AudioFolderKeyName);

                database.Tracks.Update(track);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated Track {TrackId} audio",
                    executeContext.Arguments.TrackId);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error updating Track {TrackId} audio",
                    executeContext.Arguments.TrackId);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }

        public async Task<CompensationResult> Compensate(CompensateContext<UpdateTrackAudioLog> compensateContext)
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

                track.AudioFolderKeyName = compensateContext.Log.PreviousFolderAudioKeyName;

                database.Tracks.Update(track);
                await database.SaveChangesAsync(compensateContext.CancellationToken);

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error compensating Track {TrackId} audio", compensateContext.Log.TrackId);
                return compensateContext.Failed(exception);
            }
        }
    }
}