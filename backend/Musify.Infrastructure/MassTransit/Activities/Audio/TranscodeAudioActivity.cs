using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

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
            var sourceFilePath = executeContext.GetVariable<string>(executeContext.Arguments.SourceFilePathVariable);
            ArgumentNullException.ThrowIfNull(sourceFilePath, nameof(sourceFilePath));
            var workingDirectory = executeContext.GetVariable<string>(executeContext.Arguments.WorkingDirectoryVariable);
            ArgumentNullException.ThrowIfNull(workingDirectory, nameof(workingDirectory));

            Track track;
            try
            {
                var getTrack = await database.Tracks.SingleOrDefaultAsync(t => t.Id == executeContext.Arguments.TrackId, executeContext.CancellationToken);
                track = getTrack ?? throw new InvalidOperationException($"Track with Id {executeContext.Arguments.TrackId} not found");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to retrieve track with Id {TrackId} from database", executeContext.Arguments.TrackId);
                throw;
            }

            try
            {
                TimeSpan duration;
                await using (var fileStream = File.OpenRead(sourceFilePath))
                {
                    duration = await audioTranscoder.TranscodeToAudioFileAsync(
                        fileStream,
                        workingDirectory,
                        executeContext.CancellationToken);
                }

                File.Delete(sourceFilePath);

                var log = new TranscodeAudioLog(workingDirectory);
                return executeContext.CompletedWithVariables(log, new Dictionary<string, object>
                {
                    [RoutingSlipVariableNames.Audio.DurationSeconds] = (int)Math.Round(duration.TotalSeconds)
                } );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to transcode {SourceFilePath}", sourceFilePath);

                try
                {
                    track.AudioTranscodeProcessingStatus = ProcessingStatus.Failed;
                    await database.SaveChangesAsync(executeContext.CancellationToken);
                }
                catch (Exception dbException)
                {
                    logger.LogError(dbException, "Failed to update ProcessingStatus to Failed for track {TrackId}",
                        executeContext.Arguments.TrackId);
                }

                throw;
            }
        }

        public Task<CompensationResult> Compensate(CompensateContext<TranscodeAudioLog> compensateContext)
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
