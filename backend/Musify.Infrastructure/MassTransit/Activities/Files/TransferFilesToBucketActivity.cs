using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class TransferFilesToBucketActivity(
        IStorageService storageHandler,
        ILogger<TransferFilesToBucketActivity> logger)
        : IActivity<TransferFilesToBucketArguments, TransferFilesToBucketLog>
    {
        public const string ExecuteEndpointName = "transfer-files-to-bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<TransferFilesToBucketArguments> executeContext)
        {
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
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to transfer files from {FolderPath} to {DestinationBucket}",
                    folderPath,
                    executeContext.Arguments.DestinationBucket);
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