using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files;

internal class CopyFileInBucketActivity(
    IStorageService storageService,
    ILogger<CopyFileInBucketActivity> logger)
    : IActivity<CopyFileInBucketArguments, CopyFileInBucketLog>
{
    public const string ExecuteEndpointName = "copy-file-in-bucket";

    public async Task<ExecutionResult> Execute(ExecuteContext<CopyFileInBucketArguments> executeContext)
    {
        await storageService.CopyFileAsync(
            executeContext.Arguments.SourceBucket,
            executeContext.Arguments.SourceKey,
            executeContext.Arguments.DestinationBucket,
            executeContext.Arguments.DestinationKey,
            executeContext.CancellationToken);

        return executeContext.Completed(new CopyFileInBucketLog(
            executeContext.Arguments.DestinationBucket,
            executeContext.Arguments.DestinationKey));
    }

    public async Task<CompensationResult> Compensate(CompensateContext<CopyFileInBucketLog> compensateContext)
    {
        try
        {
            await storageService.RemoveFileAsync(
                compensateContext.Log.DestinationBucket,
                compensateContext.Log.DestinationKey,
                compensateContext.CancellationToken);
            return compensateContext.Compensated();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to compensate copied file {Bucket}/{Key}",
                compensateContext.Log.DestinationBucket,
                compensateContext.Log.DestinationKey);
            return compensateContext.Failed(exception);
        }
    }
}
