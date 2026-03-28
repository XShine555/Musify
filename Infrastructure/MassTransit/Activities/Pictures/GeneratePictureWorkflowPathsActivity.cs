using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class GeneratePictureWorkflowPathsActivity(
        ILogger<GeneratePictureWorkflowPathsActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<GeneratePictureWorkflowPathsArguments>
    {
        public const string ExecuteEndpointName = "generate-picture-workflow-paths";

        internal const string ResizedPictureFileExtension = ".webp";

        public async Task<ExecutionResult> Execute(ExecuteContext<GeneratePictureWorkflowPathsArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(GeneratePictureWorkflowPathsActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            try
            {
                var destinationFolderName = Guid.NewGuid().ToString();
                var workingDirectory = Path.Combine(executeContext.Arguments.TemporaryRootDirectory, destinationFolderName);

                var sourceFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + Path.GetExtension(executeContext.Arguments.SourceKey));
                var smallPictureFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);
                var mediumPictureFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);
                var largePictureFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);

                Directory.CreateDirectory(workingDirectory);

                logger.LogDebug("Generated picture workflow paths in {WorkingDirectory}",
                    workingDirectory);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.CompletedWithVariables(new Dictionary<string, object>
                {
                    [RoutingSlipVariableNames.Workflow.TemporalDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Picture.OriginalFilePath] = sourceFilePath,
                    [RoutingSlipVariableNames.Picture.SmallResizedFilePath] = smallPictureFilePath,
                    [RoutingSlipVariableNames.Picture.MediumResizedFilePath] = mediumPictureFilePath,
                    [RoutingSlipVariableNames.Picture.LargeResizedFilePath] = largePictureFilePath,
                    [RoutingSlipVariableNames.Picture.DestinationFolderName] = destinationFolderName
                } );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate picture workflow paths for {SourceKey}",
                    executeContext.Arguments.SourceKey);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}