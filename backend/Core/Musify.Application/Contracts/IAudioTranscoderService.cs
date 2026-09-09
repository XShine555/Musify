namespace Musify.Application.Contracts
{
    public interface IAudioTranscoderService
    {
        Task<AudioTranscodeResult> TranscodeToAudioFileAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken);

        Task<bool> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken);
    }
}