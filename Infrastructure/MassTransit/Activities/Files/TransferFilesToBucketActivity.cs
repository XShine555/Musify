using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Logs;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class TransferFilesToBucketActivity(
        IStorageService storageHandler,
        ILogger<TransferFilesToBucketActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<TransferFilesToBucketArguments, TransferFilesToBucketLog>
    {
        public const string ExecuteEndpointName = "transfer-files-to-bucket";

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

            var folderPath = executeContext.GetVariable<string>(executeContext.Arguments.SourceDirectoryVariable);
            ArgumentNullException.ThrowIfNull(folderPath, nameof(folderPath));
            var destinationKey = executeContext.Arguments.DestinationKey;
            ArgumentNullException.ThrowIfNull(destinationKey, nameof(destinationKey));

            try
            {
                var uploadedKeys = Directory.GetFiles(folderPath)
                    .Select(file => Path.Combine(destinationKey, Path.GetFileName(file)))
                    .ToArray();

                await storageHandler.TransferFilesAsync(
                    folderPath,
                    executeContext.Arguments.DestinationBucket,
                    destinationKey,
                    executeContext.CancellationToken);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to transfer files from {FolderPath} to {DestinationBucket}",
                    folderPath,
                    executeContext.Arguments.DestinationBucket);
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
                        compensateContext.Log.DestinationBucket,
                        uploadedKey,
                        compensateContext.CancellationToken);
                }

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate file transfers to {DestinationBucket}", compensateContext.Log.DestinationBucket);
                return compensateContext.Failed(exception);
            }
        }
    }
}