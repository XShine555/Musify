using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Files;

internal class DownloadFileFromUrlActivity(
    IHttpClientFactory httpClientFactory,
    ILogger<DownloadFileFromUrlActivity> logger)
    : IActivity<DownloadFileFromUrlArguments, DownloadFileFromUrlLog>
{
    public const string ExecuteEndpointName = "download-file-from-url";

    public async Task<ExecutionResult> Execute(ExecuteContext<DownloadFileFromUrlArguments> executeContext)
    {
        var destinationPath = executeContext.GetVariable<string>(executeContext.Arguments.DestinationFilePathVariable);
        ArgumentNullException.ThrowIfNull(destinationPath, nameof(destinationPath));

        try
        {
            var httpClient = httpClientFactory.CreateClient();

            using var response = await httpClient.GetAsync(
                executeContext.Arguments.SourceUrl,
                HttpCompletionOption.ResponseHeadersRead,
                executeContext.CancellationToken);
            response.EnsureSuccessStatusCode();

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            await using (var destinationStream = File.Create(destinationPath))
            {
                await response.Content.CopyToAsync(destinationStream, executeContext.CancellationToken);
            }

            logger.LogInformation("Downloaded {SourceUrl} to {DestinationPath}",
                executeContext.Arguments.SourceUrl, destinationPath);

            return executeContext.Completed(new DownloadFileFromUrlLog(
                executeContext.Arguments.SourceUrl,
                destinationPath));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to download file from {SourceUrl}", executeContext.Arguments.SourceUrl);
            throw;
        }
    }

    public Task<CompensationResult> Compensate(CompensateContext<DownloadFileFromUrlLog> compensateContext)
    {
        try
        {
            if (File.Exists(compensateContext.Log.DestinationFilePath))
                File.Delete(compensateContext.Log.DestinationFilePath);
            return Task.FromResult(compensateContext.Compensated());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to compensate downloaded file {DestinationFilePath}",
                compensateContext.Log.DestinationFilePath);
            return Task.FromResult(compensateContext.Failed(exception));
        }
    }
}
