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
                var originalFileName = Path.GetFileName(executeContext.Arguments.SourceKeyName);
                var sourceFileExtension = Path.GetExtension(originalFileName);
                var workingDirectory = Path.Combine(executeContext.Arguments.TemporaryRootDirectory, destinationFolderName);


                var sourceFileName = Guid.NewGuid().ToString() + sourceFileExtension;
                var smallPictureFileName = Guid.NewGuid().ToString() + sourceFileExtension;
                var mediumPictureFileName = Guid.NewGuid().ToString() + sourceFileExtension;
                var largePictureFileName = Guid.NewGuid().ToString() + sourceFileExtension;

                var sourceFilePath = Path.Combine(workingDirectory, sourceFileName);
                var smallPictureFilePath = Path.Combine(workingDirectory, smallPictureFileName);
                var mediumPictureFilePath = Path.Combine(workingDirectory, mediumPictureFileName);
                var largePictureFilePath = Path.Combine(workingDirectory, largePictureFileName);

                Directory.CreateDirectory(workingDirectory);

                logger.LogDebug("Generated picture workflow paths. WorkingDirectory: {WorkingDirectory}, SourceFilePath: {SourceFilePath}",
                    workingDirectory,
                    sourceFilePath);

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
                logger.LogError(exception, "Error generating picture workflow paths for key {SourceKeyName}",
                    executeContext.Arguments.SourceKeyName);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}