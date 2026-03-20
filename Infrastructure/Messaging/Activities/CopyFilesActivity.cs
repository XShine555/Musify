using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class CopyFilesActivity(IStorageHandler storageHandler, ILogger<CopyFilesActivity> logger)
        : IExecuteActivity<CopyFileArguments[]>
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<CopyFileArguments[]> executeContext)
        {
            foreach (var file in executeContext.Arguments)
            {
                var copyFileResult = await storageHandler.CopyFileAsync(
                    file.SourceBucketName,
                    file.SourceKeyName,
                    file.DestinationBucketName,
                    file.SourceKeyName,
                    executeContext.CancellationToken);

                if (!copyFileResult.IsSuccess)
                {
                    string errorMessage = string.Join("; ", copyFileResult.Errors);
                    logger.LogError(
                        "Failed to copy file from {SourceBucket}/{SourceKey} to {DestinationBucket}/{DestinationKey}. Error: {ErrorMessage}",
                        file.SourceBucketName,
                        file.SourceKeyName,
                        file.DestinationBucketName,
                        file.DestinationKeyName,
                        errorMessage);
                    throw new Exception(errorMessage);
                }
            }

            return executeContext.Completed();
        }
    }
}