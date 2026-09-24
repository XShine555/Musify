using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Pictures
{
    internal class ResizePictureActivity(
        IPictureService pictureHandler,
        ILogger<ResizePictureActivity> logger)
        : IActivity<ResizePictureLocalArguments, ResizePictureLog>
    {
        public const string ExecuteEndpointName = "resize-picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<ResizePictureLocalArguments> executeContext)
        {
            var sourceFilePath = executeContext.GetVariable<string>(executeContext.Arguments.SourceFilePathVariable);
            ArgumentNullException.ThrowIfNull(sourceFilePath);
            var destinationFilePath = executeContext.GetVariable<string>(executeContext.Arguments.DestinationFilePathVariable);
            ArgumentNullException.ThrowIfNull(destinationFilePath);

            if (!File.Exists(sourceFilePath))
            {
                logger.LogWarning("Source file not found: {SourceFilePath}", sourceFilePath);
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
            }

            await using var fileStream = File.OpenRead(sourceFilePath);

            await using var resizedPicture = await pictureHandler.ResizePictureAsWebpAsync(
                fileStream,
                executeContext.Arguments.Width,
                executeContext.Arguments.Height,
                executeContext.CancellationToken);

            var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            await using var destinationStream = File.Create(destinationFilePath);
            await resizedPicture.CopyToAsync(destinationStream, executeContext.CancellationToken);

            logger.LogDebug("Resized picture from {SourceFilePath} to {DestinationFilePath}",
                sourceFilePath,
                destinationFilePath);
            return executeContext.Completed(new ResizePictureLog(destinationFilePath));
        }

        public Task<CompensationResult> Compensate(CompensateContext<ResizePictureLog> compensateContext)
        {
            try
            {
                File.Delete(compensateContext.Log.DestinationFilePath);
                return Task.FromResult(compensateContext.Compensated());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate resized file {DestinationFilePath}", compensateContext.Log.DestinationFilePath);
                return Task.FromResult(compensateContext.Failed(exception));
            }
        }
    }
}
