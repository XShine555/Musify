using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files
{
    internal class DownloadFileFromBucketActivity(
        IStorageService storageHandler,
        ILogger<DownloadFileFromBucketActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<DownloadFileFromBucketArguments, DownloadFileFromBucketLog>
    {
        public const string ExecuteEndpointName = "download-file-from-bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<DownloadFileFromBucketArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(DownloadFileFromBucketActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            var destinationPath = executeContext.GetVariable<string>(executeContext.Arguments.DestinationFilePathVariable);
            ArgumentNullException.ThrowIfNull(destinationPath, nameof(destinationPath));

            try
            {
                using var fileStream = await storageHandler.GetFileAsync(
                    executeContext.Arguments.Bucket,
                    executeContext.Arguments.Key,
                    executeContext.CancellationToken);

                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                if (fileStream.CanSeek)
                    fileStream.Position = 0;

                using var destinationStream = File.Create(destinationPath);
                await fileStream.CopyToAsync(destinationStream, executeContext.CancellationToken);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed(new DownloadFileFromBucketLog(
                    executeContext.Arguments.Bucket,
                    executeContext.Arguments.Key,
                    destinationPath));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to download file from bucket");
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }

        public Task<CompensationResult> Compensate(CompensateContext<DownloadFileFromBucketLog> compensateContext)
        {
            try
            {
                File.Delete(compensateContext.Log.DestinationFilePath);
                return Task.FromResult(compensateContext.Compensated());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate downloaded file {DestinationFilePath}", compensateContext.Log.DestinationFilePath);
                return Task.FromResult(compensateContext.Failed(exception));
            }
        }
    }
}