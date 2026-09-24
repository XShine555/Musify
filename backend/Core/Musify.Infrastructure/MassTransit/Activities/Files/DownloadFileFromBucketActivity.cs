using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files;

internal class DownloadFileFromBucketActivity(
    IStorageService storageHandler,
    ILogger<DownloadFileFromBucketActivity> logger)
    : IActivity<DownloadFileFromBucketArguments, DownloadFileFromBucketLog>
{
    public const string ExecuteEndpointName = "download-file-from-bucket";

    public async Task<ExecutionResult> Execute(ExecuteContext<DownloadFileFromBucketArguments> executeContext)
    {
        var destinationPath = executeContext.GetVariable<string>(executeContext.Arguments.DestinationFilePathVariable);
        ArgumentNullException.ThrowIfNull(destinationPath, nameof(destinationPath));

        try
        {
            using var fileStream = await storageHandler.GetFileAsync(
                executeContext.Arguments.Bucket,
                executeContext.Arguments.Key,
                executeContext.CancellationToken);

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            using var destinationStream = File.Create(destinationPath);
            await fileStream.CopyToAsync(destinationStream, executeContext.CancellationToken);
            return executeContext.Completed(new DownloadFileFromBucketLog(
                executeContext.Arguments.Bucket,
                executeContext.Arguments.Key,
                destinationPath));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to download file from bucket");
            throw;
        }
    }

    public Task<CompensationResult> Compensate(CompensateContext<DownloadFileFromBucketLog> compensateContext)
    {
        try
        {
            File.Delete(compensateContext.Log.DestinationFilePath);
            return Task.FromResult(compensateContext.Compensated());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to compensate downloaded file {DestinationFilePath}", compensateContext.Log.DestinationFilePath);
            return Task.FromResult(compensateContext.Failed(exception));
        }
    }
}
