using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Logs;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class UpdatePlayListPictureActivity(
        IDatabase database,
        ILogger<UpdatePlayListPictureActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<UpdatePlayListPictureArguments, UpdatePlayListPictureLog>
    {
        public const string ExecuteEndpointName = "update-playlist-picture";

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

                var log = new UpdatePlayListPictureLog(
                    playList.Id,
                    playList.OriginalPictureKeyName,
                    playList.SmallPictureKeyName,
                    playList.MediumPictureKeyName,
                    playList.LargePictureKeyName);

                playList.OriginalPictureKeyName = Path.GetFileName(executeContext.Arguments.OriginalPictureKeyName);
                playList.SmallPictureKeyName = Path.GetFileName(executeContext.Arguments.SmallPictureKeyName);
                playList.MediumPictureKeyName = Path.GetFileName(executeContext.Arguments.MediumPictureKeyName);
                playList.LargePictureKeyName = Path.GetFileName(executeContext.Arguments.LargePictureKeyName);

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

        public async Task<CompensationResult> Compensate(CompensateContext<UpdatePlayListPictureLog> compensateContext)
        {
            try
            {
                var playList = await database.PlayLists.FindAsync(
                    [compensateContext.Log.PlayListId],
                    cancellationToken: compensateContext.CancellationToken);

                if (playList is null)
                {
                    return compensateContext.Compensated();
                }

                playList.OriginalPictureKeyName = compensateContext.Log.PreviousOriginalPictureKeyName;
                playList.SmallPictureKeyName = compensateContext.Log.PreviousSmallPictureKeyName;
                playList.MediumPictureKeyName = compensateContext.Log.PreviousMediumPictureKeyName;
                playList.LargePictureKeyName = compensateContext.Log.PreviousLargePictureKeyName;

                database.PlayLists.Update(playList);
                await database.SaveChangesAsync(compensateContext.CancellationToken);

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error compensating PlayList {PlayListId} pictures", compensateContext.Log.PlayListId);
                return compensateContext.Failed(exception);
            }
        }
    }
}