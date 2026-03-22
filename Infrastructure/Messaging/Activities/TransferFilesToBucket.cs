using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class TransferFilesToBucket(IStorageHandler storageHandler, ILogger<TransferFilesToBucket> logger)
        : IExecuteActivity<TransferFilesToBucketArguments>
    {
        public const string ExecuteEndpointName = "Transfer-Files-To-Bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<TransferFilesToBucketArguments> executeContext)
        {
            var transferFilesResult = await storageHandler.TransferFilesAsync(
                executeContext.Arguments.FolderPath,
                executeContext.Arguments.DestinationBucketName,
                executeContext.Arguments.DestinationKeyName,
                executeContext.CancellationToken);

            if (!transferFilesResult.IsSuccess)
            {
                var errors = string.Join("; ", transferFilesResult.Errors);
                logger.LogError($"An error occurred while transferring files to bucket: {errors}");
                throw new Exception(errors);
            }

            return executeContext.Completed();
        }
    }
}