namespace Musify.Application.Contracts
{
    public interface IAudioTranscoderService
    {
        Task<int> TranscodeToAudioFileAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken);

        Task<bool> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken);
    }
}