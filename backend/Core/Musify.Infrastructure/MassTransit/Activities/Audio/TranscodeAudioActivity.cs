using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Activities.Audio
{
    internal class TranscodeAudioActivity(
        IDatabase database,
        IAudioTranscoderService audioTranscoder,
        ILogger<TranscodeAudioActivity> logger)
        : IActivity<TranscodeAudioArguments, TranscodeAudioLog>
    {
        public const string ExecuteEndpointName = "transcode-audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<TranscodeAudioArguments> executeContext)
        {
            var arguments = executeContext.Arguments;
            var sourceFilePath = executeContext.GetVariable<string>(arguments.SourceFilePathVariable);
            ArgumentNullException.ThrowIfNull(sourceFilePath);
            var workingDirectory = executeContext.GetVariable<string>(arguments.WorkingDirectoryVariable);
            ArgumentNullException.ThrowIfNull(workingDirectory);

            await TrackAudioStatus.LoadAsync(database, arguments.TrackId, executeContext.CancellationToken);

            try
            {
                AudioTranscodeResult transcodeResult;
                await using (var fileStream = File.OpenRead(sourceFilePath))
                {
                    transcodeResult = await audioTranscoder.TranscodeToAudioFileAsync(
                        fileStream, workingDirectory, executeContext.CancellationToken);
                }

                if (transcodeResult.StatusCode != 0)
                    throw new InvalidOperationException($"ffmpeg transcoding failed with exit code {transcodeResult.StatusCode}.");

                File.Delete(sourceFilePath);

                return executeContext.CompletedWithVariables(new TranscodeAudioLog(workingDirectory), new Dictionary<string, object>
                {
                    [RoutingSlipVariableNames.Audio.DurationSeconds] = transcodeResult.Duration.TotalSeconds
                });
            }
            catch
            {
                await TrackAudioStatus.MarkFailedAsync(database, arguments.TrackId, executeContext.CancellationToken);
                throw;
            }
        }

        public Task<CompensationResult> Compensate(CompensateContext<TranscodeAudioLog> compensateContext)
        {
            var workingDirectory = compensateContext.Log.WorkingDirectory;

            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, recursive: true);
            else
                logger.LogWarning("Working directory {WorkingDirectory} not found during compensation", workingDirectory);

            return Task.FromResult(compensateContext.Compensated());
        }
    }
}
