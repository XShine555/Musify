using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Musify.Infrastructure.Services
{
    public partial class AudioTranscoderService(ILogger<AudioTranscoderService> logger,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IAudioTranscoderService
    {
        record FfmpegExecutionResult(int ExitCode, string StandardOutput, string StandardError);

        [GeneratedRegex(@"Duration:\s*(\d+):(\d{2}):(\d{2}(?:\.\d+)?)", RegexOptions.IgnoreCase)]
        private static partial Regex DurationRegex();

        public async Task<AudioTranscodeResult> TranscodeToAudioFileAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(destinationPath);

                logger.LogInformation("Starting audio transcoding for {DestinationPath}", destinationPath);

                var executionResult = await ExecuteFfmpegWithInputAsync(
                    BuildAudioFileArguments(destinationPath, audioTranscoderConfiguration.Ffmpeg),
                    destinationPath,
                    audioStream,
                    "TranscodeToAudioFile",
                    cancellationToken);

                if (executionResult.ExitCode != 0)
                {
                    logger.LogError("Audio transcoding failed with exit code {ExitCode}. Output: {StandardOutput}, Error: {StandardError}",
                        executionResult.ExitCode, executionResult.StandardOutput, executionResult.StandardError);
                    return new AudioTranscodeResult(executionResult.ExitCode, TimeSpan.Zero);
                }

                logger.LogInformation("Audio transcoding completed for {DestinationPath}", destinationPath);

                var outputPath = Path.Combine(destinationPath, audioTranscoderConfiguration.Ffmpeg.OutputFileName);
                var duration = await GetAudioDurationAsync(outputPath, cancellationToken);
                return new AudioTranscodeResult(executionResult.ExitCode, duration);
            }
            catch (TimeoutException timeoutException)
            {
                logger.LogError(timeoutException, "Audio transcoding timed out after {Timeout} for {DestinationPath}", audioTranscoderConfiguration.TranscodingTimeout, destinationPath);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to transcode audio for {DestinationPath}", destinationPath);
                throw;
            }
        }

        async Task<TimeSpan> GetAudioDurationAsync(string filePath, CancellationToken cancellationToken)
        {
            var executionResult = await ExecuteFfmpegAsync(
                BuildProbeDurationArguments(filePath),
                Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory,
                "ProbeAudioDuration",
                cancellationToken);

            var match = DurationRegex().Match(executionResult.StandardError);
            if (!match.Success)
                throw new InvalidOperationException($"Could not determine audio duration for {filePath}.");

            var hours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            return new TimeSpan(hours, minutes, 0) + TimeSpan.FromSeconds(seconds);
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

            logger.LogDebug("[{Operation}] ffmpeg started (PID: {ProcessId} ) in {WorkingDirectory}", operationName, process.Id, workingDirectory);

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            if (inputStream.CanSeek)
                inputStream.Position = 0;

            await inputStream.CopyToAsync(process.StandardInput.BaseStream, cancellationToken);
            process.StandardInput.Close();

            return await WaitForProcessResultAsync(process, outputTask, errorTask, operationName, cancellationToken);
        }

        async Task<FfmpegExecutionResult> ExecuteFfmpegAsync(
            string arguments,
            string workingDirectory,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var process = BuildProcess(arguments, workingDirectory, redirectStandardInput: false);
            process.Start();

            logger.LogDebug("[{Operation}] ffmpeg started (PID: {ProcessId} ) in {WorkingDirectory}", operationName, process.Id, workingDirectory);

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            return await WaitForProcessResultAsync(process, outputTask, errorTask, operationName, cancellationToken);
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
            Task<string> outputTask,
            Task<string> errorTask,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var linkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCancellationTokens.CancelAfter(audioTranscoderConfiguration.TranscodingTimeout);

            try
            {
                await process.WaitForExitAsync(linkedCancellationTokens.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError("[{Operation}] ffmpeg timed out after {Timeout} (PID: {ProcessId} )",
                    operationName, audioTranscoderConfiguration.TranscodingTimeout, process.Id);

                TryKillProcess(process);
                throw new TimeoutException($"ffmpeg execution exceeded timeout {audioTranscoderConfiguration.TranscodingTimeout}.");
            }

            var standardError = await errorTask;
            var standardOutput = await outputTask;

            logger.LogDebug("[{Operation}] ffmpeg finished (PID: {ProcessId} ). Exit code: {ExitCode}, Output: {OutputLength}B, Error: {ErrorLength}B",
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
                logger.LogError(exception, "Failed to kill ffmpeg process (PID: {ProcessId} )", process.Id);
            }
        }

        static string BuildAudioFileArguments(string outputDirectory, FfmpegConfiguration ffmpegConfiguration)
        {
            var outputPath = Path.Combine(outputDirectory, ffmpegConfiguration.OutputFileName);
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
                   "-movflags +faststart" +
                   additionalArguments +
                   $" \"{outputPath}\"";
        }

        static string BuildValidateAudioArguments(string filePath)
            => $"-v error -i \"{filePath}\" -map 0:a:0 -f null -";

        static string BuildProbeDurationArguments(string filePath)
            => $"-hide_banner -i \"{filePath}\" -f null -";
    }
}