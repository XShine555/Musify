using MassTransit;
using Microsoft.Extensions.Logging;
using MimeMapping;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files
{
    public class UploadFileToBucketActivity(IStorageHandler storageHandler,
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
                var uploadResult = await storageHandler.UploadFileAsync(
                    sourceFilePath,
                    executeContext.Arguments.DestinationBucket,
                    Path.Combine(executeContext.Arguments.DestinationRoute, fileName),
                    executeContext.CancellationToken);

                if (!uploadResult.IsSuccess)
                {
                    var errors = string.Join("; ", uploadResult.Errors);
                    logger.LogWarning("Failed to upload file to bucket {Bucket}/{Route}: {Errors}",
                        executeContext.Arguments.DestinationBucket,
                        executeContext.Arguments.DestinationRoute,
                        errors);
                    throw new Exception(errors);
                }

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
                var uploadResult = await storageHandler.RemoveFileAsync(
                    compensateContext.Log.DestinationBucket,
                    compensateContext.Log.DestinationKey,
                    compensateContext.CancellationToken);
                
                if (!uploadResult.IsSuccess)
                {
                    var errors = string.Join("; ", uploadResult.Errors);
                    logger.LogError("Failed to remove file from bucket during compensation. Bucket: {Bucket}, Key: {Key}, Errors: {Errors}",
                        compensateContext.Log.DestinationBucket, compensateContext.Log.DestinationKey, errors);

                    return compensateContext.Failed(new Exception($"Failed to remove file from bucket during compensation. Bucket: {compensateContext.Log.DestinationBucket}, Key: {compensateContext.Log.DestinationKey}, Errors: {errors}"));
                }
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