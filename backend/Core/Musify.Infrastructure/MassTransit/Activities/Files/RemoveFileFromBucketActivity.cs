using System.Net;
using Amazon.S3;
using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Files;

internal class RemoveFileFromBucketActivity(
    IStorageService storageService,
    ILogger<RemoveFileFromBucketActivity> logger)
    : IExecuteActivity<RemoveFileFromBucketArguments>
{
    public const string ExecuteEndpointName = "remove-file-from-bucket";

    public async Task<ExecutionResult> Execute(ExecuteContext<RemoveFileFromBucketArguments> executeContext)
    {
        try
        {
            await storageService.RemoveFileAsync(
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
    }
}
