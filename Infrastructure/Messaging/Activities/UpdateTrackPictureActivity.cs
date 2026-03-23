using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class UpdateTrackPictureActivity(
        IDatabase database,
        ILogger<UpdateTrackPictureActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<UpdateTrackPictureArguments>
    {
        public const string ExecuteEndpointName = "Update-Track-Picture";

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

                track.SmallPictureKeyName = executeContext.Arguments.SmallPictureKeyName;
                track.MediumPictureKeyName = executeContext.Arguments.MediumPictureKeyName;
                track.LargePictureKeyName = executeContext.Arguments.LargePictureKeyName;

                database.Tracks.Update(track);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated Track {TrackId} pictures",
                    executeContext.Arguments.TrackId);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error updating Track {TrackId} pictures",
                    executeContext.Arguments.TrackId);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}