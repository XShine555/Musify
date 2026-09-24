namespace Musify.Application.Contracts;

public interface IDatabaseTransaction : IAsyncDisposable
{
    public Task CommitAsync(CancellationToken cancellationToken = default);
}
