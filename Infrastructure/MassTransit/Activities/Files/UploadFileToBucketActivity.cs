using MassTransit;
using Microsoft.Extensions.Logging;
using MimeMapping;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files
{
    public class UploadFileToBucketActivity(IStorageService storageHandler,
        ILogger<UploadFileToBucketActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<UploadFileToBucketArguments, UploadFileToBucketLog>
    {
        public const string ExecuteEndpointName = "upload-file-to-bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<UploadFileToBucketArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(UploadFileToBucketActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            var sourceFilePath = executeContext.GetVariable<string>(executeContext.Arguments.FilePathVariable);
            ArgumentNullException.ThrowIfNull(sourceFilePath, nameof(sourceFilePath));
            var fileName = Path.GetFileName(sourceFilePath);

            try
            {
                await storageHandler.UploadFileAsync(
                    sourceFilePath,
                    executeContext.Arguments.DestinationBucket,
                    Path.Combine(executeContext.Arguments.DestinationRoute, fileName),
                    executeContext.CancellationToken);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.CompletedWithVariables(new UploadFileToBucketLog(
                    executeContext.Arguments.DestinationBucket,
                    executeContext.Arguments.DestinationRoute));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to upload file to bucket for process {ProcessId} step {StepId}", processId, stepId);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }

        public async Task<CompensationResult> Compensate(CompensateContext<UploadFileToBucketLog> compensateContext)
        {
            try
            {
                await storageHandler.RemoveFileAsync(
                    compensateContext.Log.DestinationBucket,
                    compensateContext.Log.DestinationKey,
                    compensateContext.CancellationToken);
                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error compensating file upload for {Bucket}/{Key}",
                    compensateContext.Log.DestinationBucket,
                    compensateContext.Log.DestinationKey);
                return compensateContext.Failed(exception);
            }
        }
    }
}