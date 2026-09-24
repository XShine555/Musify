using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files;

internal class TransferFilesToBucketActivity(
    IStorageService storageService,
    ILogger<TransferFilesToBucketActivity> logger)
    : IActivity<TransferFilesToBucketArguments, TransferFilesToBucketLog>
{
    public const string ExecuteEndpointName = "transfer-files-to-bucket";

    public async Task<ExecutionResult> Execute(ExecuteContext<TransferFilesToBucketArguments> executeContext)
    {
        var folderPath = executeContext.GetVariable<string>(executeContext.Arguments.SourceDirectoryVariable);
        ArgumentNullException.ThrowIfNull(folderPath);
        var destinationKey = executeContext.Arguments.DestinationKey;
        ArgumentNullException.ThrowIfNull(destinationKey);

        var uploadedKeys = await storageService.TransferFilesAsync(
            folderPath,
            executeContext.Arguments.DestinationBucket,
            destinationKey,
            executeContext.CancellationToken);

        return executeContext.Completed(new TransferFilesToBucketLog(
            executeContext.Arguments.DestinationBucket,
            uploadedKeys.ToArray()));
    }

    public async Task<CompensationResult> Compensate(CompensateContext<TransferFilesToBucketLog> compensateContext)
    {
        try
        {
            foreach (var uploadedKey in compensateContext.Log.UploadedKeys)
            {
                await storageService.RemoveFileAsync(
                    compensateContext.Log.DestinationBucket,
                    uploadedKey,
                    compensateContext.CancellationToken);
            }

            return compensateContext.Compensated();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to compensate file transfers to {DestinationBucket}", compensateContext.Log.DestinationBucket);
            return compensateContext.Failed(exception);
        }
    }
}
