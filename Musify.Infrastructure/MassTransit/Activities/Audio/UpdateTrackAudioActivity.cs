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
    internal class UpdateTrackAudioActivity(
        IDatabase database,
        ILogger<UpdateTrackAudioActivity> logger)
        : IActivity<UpdateTrackAudioArguments, UpdateTrackAudioLog>
    {
        public const string ExecuteEndpointName = "update-track-audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdateTrackAudioArguments> executeContext)
        {
            Track track;
            try
            {
                var getTrack = await database.Tracks.SingleOrDefaultAsync(t => t.Id == executeContext.Arguments.TrackId, executeContext.CancellationToken);
                track = getTrack ?? throw new InvalidOperationException($"Track with id {executeContext.Arguments.TrackId} not found");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to retrieve track {TrackId}",
                    executeContext.Arguments.TrackId);
                throw;
            }

            var audioFolderKey = executeContext.Arguments.AudioFolderKey;
            logger.LogDebug("Resolved audio folder key: {AudioFolderKey}", audioFolderKey);

            if (string.IsNullOrWhiteSpace(audioFolderKey))
            {
                logger.LogWarning("Audio folder key is empty for track {TrackId}; skipping update", track.Id);
                return executeContext.Completed();
            }

            try
            {
                var log = new UpdateTrackAudioLog(
                    track.Id,
                    track.AudioFolderName);

                track.AudioFolderName = Path.GetFileName(audioFolderKey);
                track.AudioTranscodeProcessingStatus = ProcessingStatus.Completed;
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated track {TrackId} audio", executeContext.Arguments.TrackId);

                return executeContext.Completed(log);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update track {TrackId} audio",
                    executeContext.Arguments.TrackId);

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

        public async Task<CompensationResult> Compensate(CompensateContext<UpdateTrackAudioLog> compensateContext)
        {
            try
            {
                var track = await database.Tracks.SingleOrDefaultAsync(t => t.Id == compensateContext.Log.TrackId, compensateContext.CancellationToken);
                if (track is null)
                    throw new InvalidOperationException($"Track with id {compensateContext.Log.TrackId} not found");

                track.AudioFolderName = compensateContext.Log.PreviousAudioFolderName;
                track.AudioTranscodeProcessingStatus = ProcessingStatus.Failed;
                await database.SaveChangesAsync(compensateContext.CancellationToken);

                logger.LogInformation("Compensated track {TrackId} audio", compensateContext.Log.TrackId);

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate track {TrackId} audio",
                    compensateContext.Log.TrackId);
                return compensateContext.Failed(exception);
            }
        }
    }
}