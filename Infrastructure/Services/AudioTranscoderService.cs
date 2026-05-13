using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Infrastructure.Configuration;
using System.Diagnostics;

namespace Musify.Infrastructure.Services
{
    public class AudioTranscoderService(ILogger<AudioTranscoderService> logger,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IAudioTranscoderService
    {
        record FfmpegExecutionResult(int ExitCode, string StandardOutput, string StandardError);

        public async Task<int> TranscodeToDashAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(destinationPath);

                logger.LogInformation("Starting audio transcoding to DASH for {DestinationPath}", destinationPath);

                var executionResult = await ExecuteFfmpegWithInputAsync(
                    BuildDashArguments(destinationPath, audioTranscoderConfiguration.Ffmpeg),
                    destinationPath,
                    audioStream,
                    "TranscodeToDash",
                    cancellationToken);

                if (executionResult.ExitCode != 0)
                {
                    logger.LogError("Audio transcoding to DASH failed with exit code {ExitCode}. Output: {StandardOutput}, Error: {StandardError}",
                        executionResult.ExitCode, executionResult.StandardOutput, executionResult.StandardError);
                }
                else
                {
                    logger.LogInformation("Audio transcoding to DASH completed for {DestinationPath}", destinationPath);
                }

                return executionResult.ExitCode;
            }
            catch (TimeoutException timeoutException)
            {
                logger.LogError(timeoutException, "Audio transcoding timed out after {Timeout} for {DestinationPath}", audioTranscoderConfiguration.TranscodingTimeout, destinationPath);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to transcode audio to DASH for {DestinationPath}", destinationPath);
                throw;
            }
        }

        public async Task<bool> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("Audio validation requested for non-existing file {FilePath}", filePath);
                return false;
            }

            try
            {
                logger.LogDebug("Validating audio file {FilePath}", filePath);

                var executionResult = await ExecuteFfmpegAsync(
                    BuildValidateAudioArguments(filePath),
                    Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory,
                    "ValidateAudioFile",
                    cancellationToken);

                var isValid = executionResult.ExitCode == 0;
                if (!isValid)
                {
                    logger.LogWarning("Audio validation failed for {FilePath}: {StandardError}", filePath, executionResult.StandardError);
                }
                else
                {
                    logger.LogDebug("Audio validation successful for {FilePath}", filePath);
                }

                return isValid;
            }
            catch (TimeoutException timeoutException)
            {
                logger.LogError(timeoutException, "Audio validation timed out for {FilePath}", filePath);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to validate audio file {FilePath}", filePath);
            }
            return false;
        }

        async Task<FfmpegExecutionResult> ExecuteFfmpegWithInputAsync(
            string arguments,
            string workingDirectory,
            Stream inputStream,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var process = BuildProcess(arguments, workingDirectory, redirectStandardInput: true);
            process.Start();

            logger.LogDebug("[{Operation}] ffmpeg started (PID: {ProcessId}) in {WorkingDirectory}", operationName, process.Id, workingDirectory);

            if (inputStream.CanSeek)
                inputStream.Position = 0;

            await inputStream.CopyToAsync(process.StandardInput.BaseStream, cancellationToken);
            process.StandardInput.Close();

            return await WaitForProcessResultAsync(process, operationName, cancellationToken);
        }

        async Task<FfmpegExecutionResult> ExecuteFfmpegAsync(
            string arguments,
            string workingDirectory,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var process = BuildProcess(arguments, workingDirectory, redirectStandardInput: false);
            process.Start();

            logger.LogDebug("[{Operation}] ffmpeg started (PID: {ProcessId}) in {WorkingDirectory}", operationName, process.Id, workingDirectory);

            return await WaitForProcessResultAsync(process, operationName, cancellationToken);
        }

        Process BuildProcess(string arguments, string workingDirectory, bool redirectStandardInput)
            => new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = audioTranscoderConfiguration.Ffmpeg.ExecutableName,
                    WorkingDirectory = workingDirectory,
                    Arguments = arguments,
                    RedirectStandardInput = redirectStandardInput,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

        async Task<FfmpegExecutionResult> WaitForProcessResultAsync(
            Process process,
            string operationName,
            CancellationToken cancellationToken)
        {
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

            using var linkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCancellationTokens.CancelAfter(audioTranscoderConfiguration.TranscodingTimeout);

            try
            {
                await process.WaitForExitAsync(linkedCancellationTokens.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError("[{Operation}] ffmpeg timed out after {Timeout} (PID: {ProcessId})",
                    operationName, audioTranscoderConfiguration.TranscodingTimeout, process.Id);

                TryKillProcess(process);
                throw new TimeoutException($"ffmpeg execution exceeded timeout {audioTranscoderConfiguration.TranscodingTimeout}.");
            }

            var standardError = await errorTask;
            var standardOutput = await outputTask;

            logger.LogDebug("[{Operation}] ffmpeg finished (PID: {ProcessId}). Exit code: {ExitCode}, Output: {OutputLength}B, Error: {ErrorLength}B",
                operationName, process.Id, process.ExitCode, standardOutput.Length, standardError.Length);

            return new FfmpegExecutionResult(process.ExitCode, standardOutput, standardError);
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
                logger.LogError(exception, "Failed to kill ffmpeg process (PID: {ProcessId})", process.Id);
            }
        }

        static string BuildDashArguments(string outputDirectory, FfmpegConfiguration ffmpegConfiguration)
        {
            var manifestPath = Path.Combine(outputDirectory, ffmpegConfiguration.ManifestFileName);
            var additionalArguments = string.IsNullOrWhiteSpace(ffmpegConfiguration.AdditionalArguments)
                ? string.Empty
                : $" {ffmpegConfiguration.AdditionalArguments.Trim()}";

            return "-y -hide_banner -loglevel error " +
                   "-i pipe:0 " +
                   "-vn " +
                   "-map 0:a " +
                   $"-c:a {ffmpegConfiguration.AudioCodec} " +
                   $"-b:a {ffmpegConfiguration.AudioBitrate} " +
                   $"-ac {ffmpegConfiguration.AudioChannels} " +
                   $"-ar {ffmpegConfiguration.AudioSampleRate} " +
                   $"-profile:a {ffmpegConfiguration.AudioProfile} " +
                   "-f dash " +
                   $"-seg_duration {ffmpegConfiguration.SegmentDurationSeconds} " +
                   "-streaming 1 " +
                   "-use_template 1 " +
                   "-use_timeline 0 " +
                   $"-init_seg_name \"{ffmpegConfiguration.InitSegmentName}\" " +
                   $"-media_seg_name \"{ffmpegConfiguration.MediaSegmentName}\"" +
                   additionalArguments +
                   $" \"{manifestPath}\"";
        }

        static string BuildValidateAudioArguments(string filePath)
            => $"-v error -i \"{filePath}\" -map 0:a:0 -f null -";
    }
}