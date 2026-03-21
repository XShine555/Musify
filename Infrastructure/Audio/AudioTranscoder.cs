using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;
using System.Diagnostics;

namespace Musify.Infrastructure.Audio
{
    public class AudioTranscoder(ILogger<AudioTranscoder> logger, AudioTranscoderConfiguration transcoderConfiguration)
        : IAudioTranscoder
    {
        const string FfmpegProcessName = "ffmpeg";

        public async Task<Result> TranscodeToDash(Stream audioStream, string folderName, CancellationToken cancellationToken)
        {
            var workingDirectory = Path.Combine(transcoderConfiguration.Routes.WorkingDirectory, folderName);

            try
            {
                Directory.CreateDirectory(workingDirectory);

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = FfmpegProcessName,
                        WorkingDirectory = workingDirectory,
                        Arguments = BuildDashArguments(workingDirectory),
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();

                if (audioStream.CanSeek)
                    audioStream.Position = 0;
                await audioStream.CopyToAsync(process.StandardInput.BaseStream, cancellationToken);
                process.StandardInput.Close();

                var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

                using var linkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCancellationTokens.CancelAfter(transcoderConfiguration.TranscodingTimeout);
                await process.WaitForExitAsync(linkedCancellationTokens.Token);

                var standardError = await errorTask;
                var standardOutput = await outputTask;

                if (process.ExitCode != 0)
                {
                    logger.LogError("Audio transcoding to DASH format failed with exit code {ExitCode}. Standard Output: {StandardOutput}, Standard Error: {StandardError}", process.ExitCode, standardOutput, standardError);
                    return Result.Error($"Audio transcoding failed with exit code {process.ExitCode}. See logs for details.");
                }

                logger.LogInformation("Audio transcoding to DASH format completed successfully. Uploading to storage...");
                return Result.Success();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while transcoding audio to DASH format.");
                return Result.Error(exception.Message);
            }
        }

        static string BuildDashArguments(string outputDirectory)
        {
            var manifestPath = Path.Combine(outputDirectory, "manifest.mpd");

            return "-y -hide_banner -loglevel error " +
                   "-i pipe:0 " +
                   "-vn " +
                   "-map 0:a " +
                   "-c:a aac " +
                   "-b:a 128k " +
                   "-ac 2 " +
                   "-ar 48000 " +
                   "-profile:a aac_low " +
                   "-f dash " +
                   "-seg_duration 4 " +
                   "-streaming 1 " +
                   "-use_template 1 " +
                   "-use_timeline 0 " +
                   "-init_seg_name \"init-stream$RepresentationID$.m4s\" " +
                   "-media_seg_name \"chunk-stream$RepresentationID$-$Number$.m4s\" " +
                   $"\"{manifestPath}\"";
        }
    }
}