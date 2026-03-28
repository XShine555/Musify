using Ardalis.Result;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;
using System.Diagnostics;

namespace Musify.Infrastructure.Audio
{
    public class AudioTranscoder(
        ILogger<AudioTranscoder> logger,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IAudioTranscoder
    {
        public async Task<Result> TranscodeToDashAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(destinationPath);

                logger.LogInformation(
                    "Starting audio transcoding to DASH. DestinationPath={DestinationPath}, Timeout={Timeout}, Executable={Executable}",
                    destinationPath,
                    audioTranscoderConfiguration.TranscodingTimeout,
                    audioTranscoderConfiguration.Ffmpeg.ExecutableName);

                var executionResult = await ExecuteFfmpegWithInputAsync(
                    BuildDashArguments(destinationPath, audioTranscoderConfiguration.Ffmpeg),
                    destinationPath,
                    audioStream,
                    "TranscodeToDash",
                    cancellationToken);

                if (executionResult.ExitCode != 0)
                {
                    logger.LogError(
                        "Audio transcoding to DASH format failed with exit code={ExitCode}. Standard Output={StandardOutput}, Standard Error={StandardError}",
                        executionResult.ExitCode,
                        executionResult.StandardOutput,
                        executionResult.StandardError);
                    return Result.Error($"Audio transcoding failed with exit code: {executionResult.ExitCode}. See logs for details.");
                }

                logger.LogInformation("Audio transcoding to DASH format completed successfully. DestinationPath={DestinationPath}", destinationPath);
                return Result.Success();
            }
            catch (TimeoutException timeoutException)
            {
                logger.LogError(timeoutException, "Audio transcoding timed out after={Timeout}. DestinationPath={DestinationPath}", audioTranscoderConfiguration.TranscodingTimeout, destinationPath);
                return Result.Error("Audio transcoding timed out.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while transcoding audio to DASH format. DestinationPath={DestinationPath}", destinationPath);
                return Result.Error(exception.Message);
            }
        }

        public async Task<Result<bool>> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("Audio validation requested for a non-existing file. FilePath={FilePath}", filePath);
                return Result<bool>.Error($"Audio file not found: {filePath}");
            }

            try
            {
                logger.LogInformation("Starting audio file validation. FilePath={FilePath}, Timeout={Timeout}", filePath, audioTranscoderConfiguration.TranscodingTimeout);

                var executionResult = await ExecuteFfmpegAsync(
                    BuildValidateAudioArguments(filePath),
                    Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory,
                    "ValidateAudioFile",
                    cancellationToken);

                var isValid = executionResult.ExitCode == 0;
                if (!isValid)
                {
                    logger.LogWarning(
                        "Audio validation failed for file={FilePath}. Standard Error={StandardError}",
                        filePath,
                        executionResult.StandardError);
                }
                else
                {
                    logger.LogInformation("Audio validation completed successfully. FilePath={FilePath}", filePath);
                }

                return Result<bool>.Success(isValid);
            }
            catch (TimeoutException timeoutException)
            {
                logger.LogError(timeoutException, "Audio validation timed out after={Timeout} for file={FilePath}.", audioTranscoderConfiguration.TranscodingTimeout, filePath);
                return Result<bool>.Error("Audio validation timed out.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while validating audio file={FilePath}.", filePath);
                return Result<bool>.Error(exception.Message);
            }
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

            logger.LogDebug("[{Operation}] ffmpeg process started. ProcessId={ProcessId}, WorkingDirectory={WorkingDirectory}", operationName, process.Id, workingDirectory);

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

            logger.LogDebug("[{Operation}] ffmpeg process started. ProcessId={ProcessId}, WorkingDirectory={WorkingDirectory}", operationName, process.Id, workingDirectory);

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
                logger.LogError(
                    "[{Operation}] ffmpeg timed out after={Timeout}. ProcessId={ProcessId}",
                    operationName,
                    audioTranscoderConfiguration.TranscodingTimeout,
                    process.Id);

                TryKillProcess(process);
                throw new TimeoutException($"ffmpeg execution exceeded timeout {audioTranscoderConfiguration.TranscodingTimeout}.");
            }

            var standardError = await errorTask;
            var standardOutput = await outputTask;

            logger.LogDebug(
                "[{Operation}] ffmpeg finished. ProcessId={ProcessId}, ExitCode={ExitCode}, StdOutLength={StdOutLength}, StdErrLength={StdErrLength}",
                operationName,
                process.Id,
                process.ExitCode,
                standardOutput.Length,
                standardError.Length);

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
                logger.LogError(exception, "Failed to kill ffmpeg process. ProcessId={ProcessId}", process.Id);
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

        sealed record FfmpegExecutionResult(int ExitCode, string StandardOutput, string StandardError);
    }
}