namespace Musify.Application.Contracts
{
    public interface IDatabaseTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
