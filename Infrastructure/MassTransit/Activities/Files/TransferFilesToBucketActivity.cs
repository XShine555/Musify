using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Activities.Logs;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class TransferFilesToBucketActivity(
        IStorageHandler storageHandler,
        ILogger<TransferFilesToBucketActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<TransferFilesToBucketArguments, TransferFilesToBucketLog>
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
                nameof(TransferFilesToBucketActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            var folderPath = executeContext.GetVariable<string>(executeContext.Arguments.SourceDirectoryVariableName);
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new InvalidOperationException(
                    $"Transfer activity requires variable '{executeContext.Arguments.SourceDirectoryVariableName}' with source directory path.");

            var destinationKeyName = executeContext.Arguments.DestinationKeyName;
            if (string.IsNullOrWhiteSpace(destinationKeyName) && !string.IsNullOrWhiteSpace(executeContext.Arguments.DestinationKeyNameVariableName))
                destinationKeyName = executeContext.GetVariable<string>(executeContext.Arguments.DestinationKeyNameVariableName);

            if (string.IsNullOrWhiteSpace(destinationKeyName))
                throw new InvalidOperationException("Transfer activity requires a destination key name from arguments or variables.");

            try
            {
                var uploadedKeys = Directory.GetFiles(folderPath)
                    .Select(file => $"{destinationKeyName}/{Path.GetFileName(file)}")
                    .ToArray();

                var transferFilesResult = await storageHandler.TransferFilesAsync(
                    folderPath,
                    executeContext.Arguments.DestinationBucketName,
                    destinationKeyName,
                    executeContext.CancellationToken);

                if (!transferFilesResult.IsSuccess)
                {
                    var errorMessage = string.Join("; ", transferFilesResult.Errors);
                    logger.LogWarning("Failed to transfer files from {FolderPath} to bucket {DestinationBucketName}, key {DestinationKeyName}. Errors: {Errors}",
                        folderPath,
                        executeContext.Arguments.DestinationBucketName,
                        destinationKeyName,
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

        public async Task<CompensationResult> Compensate(CompensateContext<TransferFilesToBucketLog> compensateContext)
        {
            try
            {
                foreach (var uploadedKey in compensateContext.Log.UploadedKeys)
                {
                    await storageHandler.RemoveFileAsync(
                        compensateContext.Log.DestinationBucketName,
                        uploadedKey,
                        compensateContext.CancellationToken);
                }

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error compensating transferred files to bucket {DestinationBucketName}", compensateContext.Log.DestinationBucketName);
                return compensateContext.Failed(exception);
            }
        }
    }
}