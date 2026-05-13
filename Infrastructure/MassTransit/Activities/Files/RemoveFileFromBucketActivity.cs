using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.MassTransit.Activities.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Files
{
    internal class RemoveFileFromBucketActivity(
        IStorageService storageHandler,
        ILogger<RemoveFileFromBucketActivity> logger)
        : IExecuteActivity<RemoveFileFromBucketArguments>
    {
        public const string ExecuteEndpointName = "remove-file-from-bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<RemoveFileFromBucketArguments> executeContext)
        {
            try
            {
                await storageHandler.RemoveFileAsync(
                    executeContext.Arguments.Bucket,
                    executeContext.Arguments.Key,
                    executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to remove {Key} from bucket {Bucket}",
                    executeContext.Arguments.Key,
                    executeContext.Arguments.Bucket);
                throw;
            }
        }
    }
}