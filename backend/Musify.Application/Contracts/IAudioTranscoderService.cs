namespace Musify.Application.Contracts
{
    public interface IAudioTranscoderService
    {
        Task<TimeSpan> TranscodeToAudioFileAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken);

        Task<bool> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken);
    }
}