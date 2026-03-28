using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Logs;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class TranscodeDashAudioActivity(
        IAudioTranscoder audioTranscoder,
        ILogger<TranscodeDashAudioActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<TranscodeDashAudioArguments, TranscodeDashAudioLog>
    {
        public const string ExecuteEndpointName = "transcode-dash-audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<TranscodeDashAudioArguments> executeContext)
        {
            var processId = await processTrackingStore.GetOrCreateProcessAsync(
                "RoutingSlip",
                executeContext.CorrelationId ?? executeContext.TrackingNumber,
                executeContext.ConversationId,
                executeContext.MessageId,
                executeContext.CancellationToken);

            var stepId = await processTrackingStore.StartStepAsync(
                processId,
                nameof(TranscodeDashAudioActivity),
                ProcessStepComponentType.Activity,
                0,
                executeContext.CancellationToken);

            var sourceFilePath = executeContext.GetVariable<string>(executeContext.Arguments.SourceFilePathVariable);
            ArgumentNullException.ThrowIfNull(sourceFilePath, nameof(sourceFilePath));
            var workingDirectory = executeContext.GetVariable<string>(executeContext.Arguments.WorkingDirectoryVariable);
            ArgumentNullException.ThrowIfNull(workingDirectory, nameof(workingDirectory));

            try
            {
                await using (var fileStream = File.OpenRead(sourceFilePath))
                {
                    var result = await audioTranscoder.TranscodeToDashAsync(
                        fileStream,
                        workingDirectory,
                        executeContext.CancellationToken);

                    if (!result.IsSuccess)
                    {
                        var errorMessage = string.Join("; ", result.Errors);
                        logger.LogError("Transcoding failed for {SourceFilePath}: {Errors}",
                            sourceFilePath, errorMessage);
                        throw new Exception(errorMessage);
                    }
                }

                File.Delete(sourceFilePath);
                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to transcode {SourceFilePath}",
                    sourceFilePath);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }

        public Task<CompensationResult> Compensate(CompensateContext<TranscodeDashAudioLog> compensateContext)
        {
            try
            {
                if (Directory.Exists(compensateContext.Log.WorkingDirectory))
                    Directory.Delete(compensateContext.Log.WorkingDirectory, recursive: true);
                else
                    logger.LogWarning("Working directory {WorkingDirectory} not found during compensation",
                        compensateContext.Log.WorkingDirectory);
                return Task.FromResult(compensateContext.Compensated());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate transcoded audio directory {WorkingDirectory}", compensateContext.Log.WorkingDirectory);
                return Task.FromResult(compensateContext.Failed(exception));
            }
        }
    }
}