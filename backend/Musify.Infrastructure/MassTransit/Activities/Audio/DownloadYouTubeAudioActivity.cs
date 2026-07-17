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
    internal class DownloadYouTubeAudioActivity(
        IDatabase database,
        IYouTubeDownloaderService youTubeDownloader,
        ILogger<DownloadYouTubeAudioActivity> logger)
        : IActivity<DownloadYouTubeAudioArguments, DownloadYouTubeAudioLog>
    {
        public const string ExecuteEndpointName = "download-you-tube-audio";

        public async Task<ExecutionResult> Execute(ExecuteContext<DownloadYouTubeAudioArguments> executeContext)
        {
            var destinationPath = executeContext.GetVariable<string>(executeContext.Arguments.DestinationFilePathVariable);
            ArgumentNullException.ThrowIfNull(destinationPath, nameof(destinationPath));

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
                var downloadResult = await youTubeDownloader.DownloadAudioAsync(
                    executeContext.Arguments.VideoId,
                    destinationPath,
                    executeContext.CancellationToken);

                if (downloadResult.ExitCode != 0)
                    throw new InvalidOperationException(
                        $"yt-dlp failed with exit code {downloadResult.ExitCode}: {downloadResult.StandardError}");

                if (!File.Exists(destinationPath))
                    throw new InvalidOperationException($"yt-dlp did not produce the expected file {destinationPath}.");

                logger.LogInformation("Downloaded YouTube audio for video {VideoId} to {DestinationPath}",
                    executeContext.Arguments.VideoId, destinationPath);

                return executeContext.Completed(new DownloadYouTubeAudioLog(
                    executeContext.Arguments.VideoId,
                    destinationPath));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to download YouTube audio for video {VideoId}",
                    executeContext.Arguments.VideoId);

                try
                {
                    track.Audio.TranscodeStatus = ProcessingStatus.Failed;
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

        public Task<CompensationResult> Compensate(CompensateContext<DownloadYouTubeAudioLog> compensateContext)
        {
            try
            {
                if (File.Exists(compensateContext.Log.DestinationFilePath))
                    File.Delete(compensateContext.Log.DestinationFilePath);
                return Task.FromResult(compensateContext.Compensated());
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate downloaded YouTube audio {DestinationFilePath}",
                    compensateContext.Log.DestinationFilePath);
                return Task.FromResult(compensateContext.Failed(exception));
            }
        }
    }
}
