using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class RemoveFilesActivity(IStorageHandler storageHandler, ILogger<RemoveFilesActivity> logger)
        : IExecuteActivity<RemoveFileArguments[]>
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<RemoveFileArguments[]> executeContext)
        {
            foreach (var file in executeContext.Arguments)
            {
                var removeFile = await storageHandler.RemoveFileAsync(
                    file.BucketName,
                    file.KeyName,
                    executeContext.CancellationToken);

                if (!removeFile.IsSuccess)
                {
                    string errorMessage = string.Join(";", removeFile.Errors);
                    logger.LogWarning("Failed to remove file {File} from bucket {BucketName}: {ErrorMessage}",
                        file, file.BucketName, errorMessage);
                }
            }
            return executeContext.Completed();
        }
    }
}