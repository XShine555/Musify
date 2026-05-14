namespace Musify.Application.Contracts.Infrastructure
{
    public interface IDatabaseTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
