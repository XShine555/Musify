using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class RemoveFilesActivity(IStorageHandler storageHandler, ILogger<RemoveFilesActivity> logger)
        : IExecuteActivity<RemoveFileArguments>
    {
        public const string ExecuteEndpointName = "Remove-File";

        public async Task<ExecutionResult> Execute(ExecuteContext<RemoveFileArguments> executeContext)
        {
            var removeFile = await storageHandler.RemoveFileAsync(
                executeContext.Arguments.BucketName,
                executeContext.Arguments.KeyName,
                executeContext.CancellationToken);

            if (!removeFile.IsSuccess)
            {
                string errorMessage = string.Join(";", removeFile.Errors);
                logger.LogWarning(
                    "Failed to remove file {KeyName} from bucket {BucketName}: {ErrorMessage}",
                    executeContext.Arguments.KeyName,
                    executeContext.Arguments.BucketName,
                    errorMessage);
            }

            return executeContext.Completed();
        }
    }
}