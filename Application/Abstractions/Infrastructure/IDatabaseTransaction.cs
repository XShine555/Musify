namespace Musify.Application.Abstractions.Infrastructure
{
    public interface IDatabaseTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
