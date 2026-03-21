using Ardalis.Result;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IAudioTranscoder
    {
        Task<Result> TranscodeToDash(Stream audioStream, string folderName, CancellationToken cancellationToken);
    }
}