using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class GenerateAudioWorkflowPathsActivity(
        ILogger<GenerateAudioWorkflowPathsActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IExecuteActivity<GenerateAudioWorkflowPathsArguments>
    {
        public const string ExecuteEndpointName = "Generate-Audio-Workflow-Paths";

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
                var sourceFileName = Path.GetFileName(executeContext.Arguments.SourceKeyName);
                var workingDirectory = Path.Combine(executeContext.Arguments.TemporaryRootDirectory, folderName);
                var sourceFilePath = Path.Combine(workingDirectory, sourceFileName);

                Directory.CreateDirectory(workingDirectory);

                logger.LogDebug("Generated audio workflow paths. WorkingDirectory: {WorkingDirectory}, SourceFilePath: {SourceFilePath}",
                    workingDirectory,
                    sourceFilePath);

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.CompletedWithVariables(new Dictionary<string, object>
                {
                    [RoutingSlipVariableNames.Workflow.TempDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Audio.WorkingDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Audio.SourceFilePath] = sourceFilePath,
                    [RoutingSlipVariableNames.Audio.TranscodedDirectory] = workingDirectory
                } );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error generating audio workflow paths for key {SourceKeyName}",
                    executeContext.Arguments.SourceKeyName);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}
