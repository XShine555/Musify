using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class UpdatePlayListPictureActivity(
        IDatabase database,
        ILogger<UpdatePlayListPictureActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<UpdatePlayListPictureArguments>
    {
        public const string ExecuteEndpointName = "Update-PlayList-Picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdatePlayListPictureArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(UpdatePlayListPictureActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            try
            {
                var playList = await database.PlayLists.FindAsync(
                    [executeContext.Arguments.PlayListId],
                    cancellationToken: executeContext.CancellationToken);

                if (playList is null)
                {
                    logger.LogWarning("PlayList with id {PlayListId} not found",
                        executeContext.Arguments.PlayListId);
                    throw new InvalidOperationException($"PlayList with id {executeContext.Arguments.PlayListId} not found");
                }

                playList.SmallPictureKeyName = executeContext.Arguments.SmallPictureKeyName;
                playList.MediumPictureKeyName = executeContext.Arguments.MediumPictureKeyName;
                playList.LargePictureKeyName = executeContext.Arguments.LargePictureKeyName;

                database.PlayLists.Update(playList);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated PlayList {PlayListId} pictures",
                    executeContext.Arguments.PlayListId);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error updating PlayList {PlayListId} pictures",
                    executeContext.Arguments.PlayListId);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}