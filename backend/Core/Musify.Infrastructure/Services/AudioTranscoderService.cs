using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Services
{
    public sealed partial class AudioTranscoderService(
        ILogger<AudioTranscoderService> logger,
        AudioConfiguration audioConfiguration)
        : IAudioTranscoderService
    {
        private sealed record FfmpegExecutionResult(int ExitCode, string StandardOutput, string StandardError);

        [GeneratedRegex(@"Duration:\s*(\d+):(\d{2}):(\d{2}(?:\.\d+)?)", RegexOptions.IgnoreCase)]
        private static partial Regex DurationRegex();

        public async Task<AudioTranscodeResult> TranscodeToAudioFileAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(destinationPath);

            logger.LogInformation("Starting audio transcoding for {DestinationPath}", destinationPath);

            var executionResult = await ExecuteFfmpegAsync(
                BuildAudioFileArguments(destinationPath, audioConfiguration.Ffmpeg),
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

            var outputPath = Path.Combine(destinationPath, audioConfiguration.Ffmpeg.OutputFileName);
            var duration = await GetAudioDurationAsync(outputPath, cancellationToken);
            return new AudioTranscodeResult(executionResult.ExitCode, duration);
        }

        private async Task<TimeSpan> GetAudioDurationAsync(string filePath, CancellationToken cancellationToken)
        {
            var executionResult = await ExecuteFfmpegAsync(
                BuildProbeDurationArguments(filePath),
                Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory,
                input: null,
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

        private async Task<FfmpegExecutionResult> ExecuteFfmpegAsync(
            string arguments,
            string workingDirectory,
            Stream? input,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var process = BuildProcess(arguments, workingDirectory, redirectStandardInput: input != null);
            process.Start();

            logger.LogDebug("[{Operation}] ffmpeg started (PID: {ProcessId}) in {WorkingDirectory}", operationName, process.Id, workingDirectory);

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            if (input != null)
            {
                if (input.CanSeek)
                    input.Position = 0;

                await input.CopyToAsync(process.StandardInput.BaseStream, cancellationToken);
                process.StandardInput.Close();
            }

            return await WaitForProcessResultAsync(process, outputTask, errorTask, operationName, cancellationToken);
        }

        private Process BuildProcess(string arguments, string workingDirectory, bool redirectStandardInput)
            => new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = audioConfiguration.Ffmpeg.ExecutableName,
                    WorkingDirectory = workingDirectory,
                    Arguments = arguments,
                    RedirectStandardInput = redirectStandardInput,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

        private async Task<FfmpegExecutionResult> WaitForProcessResultAsync(
            Process process,
            Task<string> outputTask,
            Task<string> errorTask,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var linkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCancellationTokens.CancelAfter(audioConfiguration.TranscodingTimeout);

            try
            {
                await process.WaitForExitAsync(linkedCancellationTokens.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKillProcess(process);
                throw new TimeoutException($"ffmpeg execution exceeded timeout {audioConfiguration.TranscodingTimeout} (PID: {process.Id}).");
            }

            var standardError = await errorTask;
            var standardOutput = await outputTask;

            logger.LogDebug("[{Operation}] ffmpeg finished (PID: {ProcessId}). Exit code: {ExitCode}, Output: {OutputLength}B, Error: {ErrorLength}B",
                operationName, process.Id, process.ExitCode, standardOutput.Length, standardError.Length);

            return new FfmpegExecutionResult(process.ExitCode, standardOutput, standardError);
        }

        private void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch (InvalidOperationException exception)
            {
                logger.LogWarning(exception, "Could not kill ffmpeg process (PID: {ProcessId}); it had already exited", process.Id);
            }
        }

        private static string BuildAudioFileArguments(string outputDirectory, FfmpegConfiguration ffmpegConfiguration)
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

        private static string BuildProbeDurationArguments(string filePath)
            => $"-hide_banner -i \"{filePath}\" -f null -";
    }
}
