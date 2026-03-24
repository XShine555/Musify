using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Activities.Logs;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class TranscodeDashAudioActivity(
        IAudioTranscoder audioTranscoder,
        ILogger<TranscodeDashAudioActivity> logger,
        IProcessTrackingStore processTrackingStore)
        : IActivity<TranscodeDashAudioArguments, TranscodeDashAudioLog>
    {
        public const string ExecuteEndpointName = "Transcode-Dash-Audio";

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

            var sourceFilePath = executeContext.GetVariable<string>(executeContext.Arguments.SourceFilePathVariableName);
            var workingDirectory = executeContext.GetVariable<string>(executeContext.Arguments.WorkingDirectoryVariableName);

            if (string.IsNullOrWhiteSpace(sourceFilePath) || string.IsNullOrWhiteSpace(workingDirectory))
                throw new InvalidOperationException("Transcode activity requires source file path and working directory variables.");

            try
            {
                await using var fileStream = File.OpenRead(sourceFilePath);

                var result = await audioTranscoder.TranscodeToDashAsync(
                    fileStream,
                    workingDirectory,
                    executeContext.CancellationToken);

                if (!result.IsSuccess)
                {
                    var errorMessage = string.Join("; ", result.Errors);
                    logger.LogError("Transcoding failed for file {SourceFilePath}. Errors: {ErrorMessage}",
                        sourceFilePath, errorMessage);
                    throw new Exception(errorMessage);
                }

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error during transcoding activity for file {SourceFilePath}",
                    sourceFilePath);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }

        public Task<CompensationResult> Compensate(CompensateContext<TranscodeDashAudioLog> compensateContext)
        {
            try
            {
                Directory.Delete(compensateContext.Log.WorkingDirectory, recursive: true);
                return Task.FromResult(compensateContext.Compensated());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error compensating transcoded audio directory {WorkingDirectory}", compensateContext.Log.WorkingDirectory);
                return Task.FromResult(compensateContext.Failed(exception));
            }
        }
    }
}