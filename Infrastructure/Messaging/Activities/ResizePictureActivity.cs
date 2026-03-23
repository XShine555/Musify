using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class ResizePictureActivity(
        IPictureHandler pictureHandler,
        ILogger<ResizePictureActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<ResizePictureLocalArguments>
    {
        public const string ExecuteEndpointName = "Resize-Picture";

        public async Task<ExecutionResult> Execute(ExecuteContext<ResizePictureLocalArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(ResizePictureActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            try
            {
                if (!File.Exists(executeContext.Arguments.SourceFilePath))
                {
                    logger.LogError("Source file not found at {SourceFilePath}",
                        executeContext.Arguments.SourceFilePath);
                    throw new FileNotFoundException($"Source file not found: {executeContext.Arguments.SourceFilePath}");
                }

                await using var fileStream = File.OpenRead(executeContext.Arguments.SourceFilePath);

                var resizedPicture = await pictureHandler.ResizePictureAsync(
                    fileStream,
                    executeContext.Arguments.Width,
                    executeContext.Arguments.Height,
                    executeContext.CancellationToken);

                if (!resizedPicture.IsSuccess)
                {
                    var errorMessage = string.Join("; ", resizedPicture.Errors);
                    logger.LogError("Failed to resize picture {SourceFilePath} to {Width}x{Height}. Errors: {Errors}",
                        executeContext.Arguments.SourceFilePath,
                        executeContext.Arguments.Width,
                        executeContext.Arguments.Height,
                        errorMessage);
                    throw new Exception(errorMessage);
                }

                var destinationDirectory = Path.GetDirectoryName(executeContext.Arguments.DestinationFilePath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                await using var destinationStream = File.Create(executeContext.Arguments.DestinationFilePath);
                await resizedPicture.Value.CopyToAsync(destinationStream, executeContext.CancellationToken);

                logger.LogInformation("Picture resized successfully from {SourceFilePath} to {DestinationFilePath} ( {Width}x{Height} )",
                    executeContext.Arguments.SourceFilePath,
                    executeContext.Arguments.DestinationFilePath,
                    executeContext.Arguments.Width,
                    executeContext.Arguments.Height);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error resizing picture {SourceFilePath}",
                    executeContext.Arguments.SourceFilePath);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}