using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class DownloadFileFromBucketActivity(
        IStorageHandler storageHandler,
        ILogger<DownloadFileFromBucketActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<DownloadFileFromBucketArguments>
    {
        public const string ExecuteEndpointName = "Download-File-From-Bucket";

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

            try
            {
                var getFile = await storageHandler.GetFileAsync(
                    executeContext.Arguments.BucketName,
                    executeContext.Arguments.KeyName,
                    executeContext.CancellationToken);

                if (!getFile.IsSuccess)
                {
                    var errorMessage = string.Join("; ", getFile.Errors);
                    logger.LogError("Failed to download file from bucket. Bucket: {BucketName}, Key: {KeyName}, Errors: {Errors}",
                        executeContext.Arguments.BucketName,
                        executeContext.Arguments.KeyName,
                        errorMessage);
                    throw new Exception(errorMessage);
                }

                using var fileStream = getFile.Value;
                if (fileStream.CanSeek)
                    fileStream.Position = 0;

                var fileName = Path.GetFileName(executeContext.Arguments.KeyName);
                var folderName = Guid.NewGuid().ToString();
                var workingDirectory = Path.Combine(executeContext.Arguments.DestinationPath, folderName);
                var destinationPath = Path.Combine(workingDirectory, fileName);

                Directory.CreateDirectory(workingDirectory);

                using var destinationStream = File.Create(destinationPath);
                await fileStream.CopyToAsync(destinationStream, executeContext.CancellationToken);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.CompletedWithVariables(new
                {
                    SourceFilePath = destinationPath,
                    WorkingDirectory = workingDirectory,
                    DestinationFolderName = folderName
                } );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error downloading file from bucket");
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}