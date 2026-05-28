using MassTransit;
using Amazon.S3;
using Microsoft.Extensions.Logging;
using Musify.Infrastructure.MassTransit.Arguments;
using System.Net;
using Musify.Application.Contracts;

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
            catch (AmazonS3Exception exception) when (
                exception.StatusCode == HttpStatusCode.NotFound
                || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation(
                    "Key {Key} not found in bucket {Bucket}, skipping",
                    executeContext.Arguments.Key,
                    executeContext.Arguments.Bucket);
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