using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class TransferFilesToBucket(
        IStorageHandler storageHandler,
        ILogger<TransferFilesToBucket> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<TransferFilesToBucketArguments>
    {
        public const string ExecuteEndpointName = "Transfer-Files-To-Bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<TransferFilesToBucketArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(TransferFilesToBucket),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            var folderPath = executeContext.GetVariable<string>("WorkingDirectory");
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new InvalidOperationException("Transfer activity requires WorkingDirectory variable.");
            }

            try
            {
                var transferFilesResult = await storageHandler.TransferFilesAsync(
                    folderPath,
                    executeContext.Arguments.DestinationBucketName,
                    executeContext.Arguments.DestinationKeyName,
                    executeContext.CancellationToken);

                if (!transferFilesResult.IsSuccess)
                {
                    var errorMessage = string.Join("; ", transferFilesResult.Errors);
                    logger.LogError("Failed to transfer files from {FolderPath} to bucket {DestinationBucketName}, key {DestinationKeyName}. Errors: {Errors}",
                        folderPath,
                        executeContext.Arguments.DestinationBucketName,
                        executeContext.Arguments.DestinationKeyName,
                        errorMessage);
                    throw new Exception(errorMessage);
                }

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error transferring files from {FolderPath} to bucket {DestinationBucketName}",
                    folderPath,
                    executeContext.Arguments.DestinationBucketName);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}