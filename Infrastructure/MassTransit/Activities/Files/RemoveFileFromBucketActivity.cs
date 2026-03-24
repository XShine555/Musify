using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class RemoveFileFromBucketActivity(
        IStorageHandler storageHandler,
        ILogger<RemoveFileFromBucketActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<RemoveFileFromBucketArguments>
    {
        public const string ExecuteEndpointName = "Remove-File-From-Bucket";

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
                    executeContext.Arguments.BucketName,
                    executeContext.Arguments.KeyName,
                    executeContext.CancellationToken);

                if (!removeFile.IsSuccess)
                {
                    var errorMessage = string.Join("; ", removeFile.Errors);
                    logger.LogWarning("Failed to remove file {KeyName} from bucket {BucketName}. Errors: {Errors}",
                        executeContext.Arguments.KeyName,
                        executeContext.Arguments.BucketName,
                        errorMessage);
                }

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error removing file {KeyName} from bucket {BucketName}",
                    executeContext.Arguments.KeyName,
                    executeContext.Arguments.BucketName);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}