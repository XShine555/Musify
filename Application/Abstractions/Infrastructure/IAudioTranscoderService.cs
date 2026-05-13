namespace Musify.Application.Abstractions.Infrastructure
{
    public interface IAudioTranscoderService
    {
        Task<int> TranscodeToDashAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken);

        Task<bool> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken);
    }
}