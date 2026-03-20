using MassTransit;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class UpdatePlayListPictureActivity(IDatabase database, IProcessTrackingStore processTrackingStore)
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
                var playList = await database.PlayLists.FindAsync(executeContext.Arguments.PlayListId, executeContext.CancellationToken);

                if (playList is null)
                    throw new Exception($"PlayList with id {executeContext.Arguments.PlayListId} not found.");

                playList.SmallPictureKeyName = executeContext.Arguments.SmallPictureKeyName;
                playList.MediumPictureKeyName = executeContext.Arguments.MediumPictureKeyName;
                playList.LargePictureKeyName = executeContext.Arguments.LargePictureKeyName;

                database.PlayLists.Update(playList);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}