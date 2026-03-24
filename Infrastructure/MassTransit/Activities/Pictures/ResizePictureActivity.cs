using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Activities.Logs;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class ResizePictureActivity(
        IPictureHandler pictureHandler,
        ILogger<ResizePictureActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<ResizePictureLocalArguments, ResizePictureLog>
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

            var sourceFilePath = executeContext.GetVariable<string>(executeContext.Arguments.SourceFilePathVariableName);
            var destinationFilePath = executeContext.GetVariable<string>(executeContext.Arguments.DestinationFilePathVariableName);

            if (string.IsNullOrWhiteSpace(sourceFilePath) || string.IsNullOrWhiteSpace(destinationFilePath))
                throw new InvalidOperationException("Resize activity requires source and destination file path variables.");

            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    logger.LogWarning("Source file not found at {SourceFilePath}", sourceFilePath);
                    throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
                }

                await using var fileStream = File.OpenRead(sourceFilePath);

                var resizedPicture = await pictureHandler.ResizePictureAsync(
                    fileStream,
                    executeContext.Arguments.Width,
                    executeContext.Arguments.Height,
                    executeContext.CancellationToken);

                if (!resizedPicture.IsSuccess)
                {
                    var errorMessage = string.Join("; ", resizedPicture.Errors);
                    logger.LogError("Failed to resize picture {SourceFilePath} to {Width}x{Height}. Errors: {Errors}",
                        sourceFilePath,
                        executeContext.Arguments.Width,
                        executeContext.Arguments.Height,
                        errorMessage);
                    throw new Exception(errorMessage);
                }

                var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                await using var destinationStream = File.Create(destinationFilePath);
                await resizedPicture.Value.CopyToAsync(destinationStream, executeContext.CancellationToken);

                logger.LogDebug("Picture resized from {SourceFilePath} to {DestinationFilePath} ({Width}x{Height})",
                    sourceFilePath,
                    destinationFilePath,
                    executeContext.Arguments.Width,
                    executeContext.Arguments.Height);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error resizing picture {SourceFilePath}", sourceFilePath);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
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
                logger.LogError(exception, "Error compensating resized file {DestinationFilePath}", compensateContext.Log.DestinationFilePath);
                return Task.FromResult(compensateContext.Failed(exception));
            }
        }
    }
}