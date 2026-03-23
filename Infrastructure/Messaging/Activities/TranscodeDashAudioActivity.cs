using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class TranscodeDashAudioActivity(
        IAudioTranscoder audioTranscoder,
        ILogger<TranscodeDashAudioActivity> logger,
        IProcessTrackingStore processTrackingStore,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IExecuteActivity<TranscodeDashAudioArguments>
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

            try
            {
                var workingDirectory = Path.Combine(
                    audioTranscoderConfiguration.Routes.WorkingDirectory,
                    executeContext.Arguments.DestinationFolderName);

                await using var fileStream = File.OpenRead(executeContext.Arguments.SourceFilePath);

                var result = await audioTranscoder.TranscodeToDashAsync(
                    fileStream,
                    executeContext.Arguments.DestinationFolderName,
                    audioTranscoderConfiguration.TranscodingTimeout,
                    executeContext.CancellationToken);

                if (!result.IsSuccess)
                {
                    var errorMessage = string.Join("; ", result.Errors);
                    logger.LogError("Transcoding failed for file {SourceFilePath}. Errors: {ErrorMessage}",
                        executeContext.Arguments.SourceFilePath, errorMessage);
                    throw new Exception(errorMessage);
                }

                await processTrackingStore.CompleteStepAsync(processId, stepId, executeContext.CancellationToken);
                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error during transcoding activity for file {SourceFilePath}",
                    executeContext.Arguments.SourceFilePath);
                await processTrackingStore.FailStepAsync(processId, stepId, exception.Message, executeContext.CancellationToken);
                throw;
            }
        }
    }
}