using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IAudioTranscoder
    {
        Task<Result> TranscodeToDashAsync(Stream audioStream, string destinationPath, CancellationToken cancellationToken);

        Task<Result<bool>> IsValidAudioFileAsync(string filePath, CancellationToken cancellationToken);
    }
}