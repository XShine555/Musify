using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Activities.Logs;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class DownloadFileFromBucketActivity(IStorageHandler storageHandler, ILogger<DownloadFileFromBucketActivity> logger)
        : IExecuteActivity<DownloadFileFromBucketArguments>
    {
        public const string ExecuteEndpointName = "Download-File-From-Bucket";

        public async Task<ExecutionResult> Execute(ExecuteContext<DownloadFileFromBucketArguments> executeContext)
        {
            var getFile = await storageHandler.GetFileAsync(
                executeContext.Arguments.BucketName,
                executeContext.Arguments.KeyName,
                executeContext.CancellationToken);

            if (!getFile.IsSuccess)
            {
                var error = string.Join("; ", getFile.Errors);
                logger.LogError("Failed to download file from bucket. Bucket: {BucketName}, Key: {KeyName}",
                    executeContext.Arguments.BucketName,
                    executeContext.Arguments.KeyName);
                throw new Exception(error);
            }

            using var fileStream = getFile.Value;
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            var fileName = Path.GetFileName(executeContext.Arguments.KeyName);
            using var destinationStream = File.Create($"D:\\tempsFilesDev\\{fileName}");
            await fileStream.CopyToAsync(destinationStream, executeContext.CancellationToken);

            return executeContext.Completed();
        }
    }
}