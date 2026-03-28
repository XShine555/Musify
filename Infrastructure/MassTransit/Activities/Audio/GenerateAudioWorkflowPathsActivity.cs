using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class GenerateAudioWorkflowPathsActivity(
        ILogger<GenerateAudioWorkflowPathsActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<GenerateAudioWorkflowPathsArguments>
    {
        public const string ExecuteEndpointName = "generate-audio-workflow-paths";

        public async Task<ExecutionResult> Execute(ExecuteContext<GenerateAudioWorkflowPathsArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(GenerateAudioWorkflowPathsActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            try
            {
                var folderName = Guid.NewGuid().ToString();
                var sourceFileName = Path.GetFileName(executeContext.Arguments.SourceKey);
                var workingDirectory = Path.Combine(executeContext.Arguments.TemporaryRootDirectory, folderName);
                var sourceFilePath = Path.Combine(workingDirectory, sourceFileName);

                Directory.CreateDirectory(workingDirectory);

                logger.LogDebug("Generated audio workflow paths in {WorkingDirectory}",
                    workingDirectory);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.CompletedWithVariables(new Dictionary<string, object>
                {
                    [RoutingSlipVariableNames.Workflow.TemporalDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Audio.WorkingDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Audio.SourceFilePath] = sourceFilePath,
                    [RoutingSlipVariableNames.Audio.TranscodedDirectory] = workingDirectory
                } );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate audio workflow paths for {SourceKey}",
                    executeContext.Arguments.SourceKey);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}
