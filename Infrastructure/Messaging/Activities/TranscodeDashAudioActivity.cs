using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Activities.Logs;

namespace Musify.Infrastructure.Messaging.Activities
{
    public class TranscodeDashAudioActivity(IAudioTranscoder audioTranscoder, ILogger<TranscodeDashAudioActivity> logger,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IExecuteActivity<TranscodeDashAudioArguments>
    {
        public const string ExecuteEndpointName = "Transcode-Dash-Audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<TranscodeDashAudioArguments> executeContext)
        {
            var workingDirectory = Path.Combine(
                audioTranscoderConfiguration.Routes.WorkingDirectory,
                executeContext.Arguments.DestinationFolderName);

            try
            {
                await using var fileStream = File.OpenRead(executeContext.Arguments.SourceFilePath);

                var result = await audioTranscoder.TranscodeToDashAsync(
                    fileStream,
                    executeContext.Arguments.DestinationFolderName,
                    audioTranscoderConfiguration.TranscodingTimeout,
                    executeContext.CancellationToken);

                if (!result.IsSuccess)
                {
                    var errors = string.Join(";  ", result.Errors); 
                    logger.LogError("transcoding failed for file {SourceFilePath}. Error: {ErrorMessage}",
                        executeContext.Arguments.SourceFilePath, errors);
                    throw new Exception(errors);
                }

                return executeContext.Completed();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error during transcoding activity.");
                throw;
            }
        }
    }
}