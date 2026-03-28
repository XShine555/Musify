using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class RemoveFileFromBucketActivity(
        IStorageHandler storageHandler,
        ILogger<RemoveFileFromBucketActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<RemoveFileFromBucketArguments>
    {
        public const string ExecuteEndpointName = "remove-file-from-bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<RemoveFileFromBucketArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(RemoveFileFromBucketActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            try
            {
                var removeFile = await storageHandler.RemoveFileAsync(
                    executeContext.Arguments.Bucket,
                    executeContext.Arguments.Key,
                    executeContext.CancellationToken);

                if (!removeFile.IsSuccess)
                {
                    var errorMessage = string.Join("; ", removeFile.Errors);
                    logger.LogWarning("Failed to remove {Key} from bucket {Bucket}: {Errors}",
                        executeContext.Arguments.Key,
                        executeContext.Arguments.Bucket,
                        errorMessage);
                }

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to remove {Key} from bucket {Bucket}",
                    executeContext.Arguments.Key,
                    executeContext.Arguments.Bucket);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}