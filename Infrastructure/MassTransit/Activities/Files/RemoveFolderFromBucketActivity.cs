using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Files
{
    internal class RemoveFolderFromBucketActivity(
        IStorageService storageHandler,
        ILogger<RemoveFolderFromBucketActivity> logger)
        : IExecuteActivity<RemoveFolderFromBucketArguments>
    {
        public const string ExecuteEndpointName = "remove-folder-from-bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<RemoveFolderFromBucketArguments> executeContext)
        {
            var folderKey = executeContext.GetVariable<string>(executeContext.Arguments.FolderKeyVariable);
            ArgumentException.ThrowIfNullOrEmpty(folderKey, nameof(folderKey));

            try
            {
                await storageHandler.RemoveFolderAsync(
                    executeContext.Arguments.Bucket,
                    folderKey,
                    executeContext.CancellationToken);

                logger.LogInformation("Removed folder {FolderKey} from bucket {Bucket}",
                    folderKey,
                    executeContext.Arguments.Bucket);

                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to remove folder {FolderKey} from bucket {Bucket}",
                    folderKey,
                    executeContext.Arguments.Bucket);
                throw;
            }
        }
    }
}