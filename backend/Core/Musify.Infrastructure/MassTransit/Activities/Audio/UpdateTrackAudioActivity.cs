using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Audio
{
    internal class UpdateTrackAudioActivity(
        IDatabase database,
        IPublishEndpoint publishEndpoint,
        ILogger<UpdateTrackAudioActivity> logger)
        : IActivity<UpdateTrackAudioArguments, UpdateTrackAudioLog>
    {
        public const string ExecuteEndpointName = "update-track-audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdateTrackAudioArguments> executeContext)
        {
            var arguments = executeContext.Arguments;
            var track = await TrackAudioStatus.LoadAsync(database, arguments.TrackId, executeContext.CancellationToken);

            try
            {
                if (string.IsNullOrWhiteSpace(arguments.AudioFolderKey))
                    throw new InvalidOperationException($"Audio folder key is empty for track {track.Id}");

                var durationSeconds = executeContext.GetVariable<double>(arguments.DurationSecondsVariable)
                    ?? throw new InvalidOperationException($"Missing routing slip variable {arguments.DurationSecondsVariable}");

                var log = new UpdateTrackAudioLog(track.Id, track.Audio.FolderName, track.DurationSeconds);

                track.Audio.FolderName = Path.GetFileName(arguments.AudioFolderKey);
                track.DurationSeconds = durationSeconds;
                track.Audio.TranscodeStatus = ProcessingStatus.Completed;
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogInformation("Updated track {TrackId} audio", arguments.TrackId);

                await publishEndpoint.Publish(new TrackAudioProcessed(arguments.TrackId), executeContext.CancellationToken);

                return executeContext.Completed(log);
            }
            catch
            {
                await TrackAudioStatus.MarkFailedAsync(database, arguments.TrackId, executeContext.CancellationToken);
                throw;
            }
        }

        public async Task<CompensationResult> Compensate(CompensateContext<UpdateTrackAudioLog> compensateContext)
        {
            var log = compensateContext.Log;

            var track = await TrackAudioStatus.LoadAsync(database, log.TrackId, compensateContext.CancellationToken);
            track.Audio.FolderName = log.PreviousAudioFolderName;
            track.DurationSeconds = log.PreviousDurationSeconds;
            track.Audio.TranscodeStatus = ProcessingStatus.Failed;
            await database.SaveChangesAsync(compensateContext.CancellationToken);

            logger.LogInformation("Compensated track {TrackId} audio", log.TrackId);

            return compensateContext.Compensated();
        }
    }
}
