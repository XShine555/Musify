using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IAudioTranscoder
    {
        Task<Result> TranscodeToDashAsync(Stream audioStream, string destinationPath, TimeSpan timeOut, CancellationToken cancellationToken);
    }
}