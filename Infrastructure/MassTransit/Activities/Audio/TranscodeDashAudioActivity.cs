using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Logs;

namespace Musify.Infrastructure.MassTransit.Activities
{
    internal class TranscodeDashAudioActivity(
        IDatabase database,
        IAudioTranscoderService audioTranscoder,
        ILogger<TranscodeDashAudioActivity> logger)
        : IActivity<TranscodeDashAudioArguments, TranscodeDashAudioLog>
    {
        public const string ExecuteEndpointName = "transcode-dash-audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<TranscodeDashAudioArguments> executeContext)
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
                await using var fileStream = File.OpenRead(sourceFilePath);
                await audioTranscoder.TranscodeToDashAsync(
                    fileStream,
                    workingDirectory,
                    executeContext.CancellationToken);

                File.Delete(sourceFilePath);
                return executeContext.Completed();
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