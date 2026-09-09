using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using System.Diagnostics;

namespace Musify.Infrastructure.Services
{
    public class YouTubeDownloaderService(
        ILogger<YouTubeDownloaderService> logger,
        YtDlpConfiguration configuration)
        : IYouTubeDownloaderService
    {
        public async Task<YouTubeDownloadResult> DownloadAudioAsync(string videoId, string destinationFilePath, CancellationToken cancellationToken)
        {
            var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            var additionalArguments = string.IsNullOrWhiteSpace(configuration.AdditionalArguments)
                ? string.Empty
                : $" {configuration.AdditionalArguments.Trim()}";

            var arguments =
                $"-f {configuration.Format} " +
                "--no-playlist " +
                $"-o \"{destinationFilePath}\"" +
                additionalArguments +
                $" -- {videoId}";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = configuration.ExecutableName,
                    WorkingDirectory = destinationDirectory ?? Environment.CurrentDirectory,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            logger.LogInformation("yt-dlp started (PID: {ProcessId} ) for video {VideoId}", process.Id, videoId);

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var linkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCancellationTokens.CancelAfter(configuration.DownloadTimeout);

            try
            {
                await process.WaitForExitAsync(linkedCancellationTokens.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError("yt-dlp timed out after {Timeout} for video {VideoId} (PID: {ProcessId} )",
                    configuration.DownloadTimeout, videoId, process.Id);

                TryKillProcess(process);
                throw new TimeoutException($"yt-dlp execution exceeded timeout {configuration.DownloadTimeout}.");
            }

            var standardError = await errorTask;
            var standardOutput = await outputTask;

            logger.LogDebug("yt-dlp finished (PID: {ProcessId} ). Exit code: {ExitCode}, Output: {OutputLength}B, Error: {ErrorLength}B",
                process.Id, process.ExitCode, standardOutput.Length, standardError.Length);

            return new YouTubeDownloadResult(process.ExitCode, standardError);
        }

        void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to kill yt-dlp process (PID: {ProcessId} )", process.Id);
            }
        }
    }
}
