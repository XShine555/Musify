namespace Musify.Application.Contracts;

public interface IAudioTranscoderService
{
    public Task<AudioTranscodeResult> TranscodeToAudioFileAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken);

    public Task<bool> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken);
}
