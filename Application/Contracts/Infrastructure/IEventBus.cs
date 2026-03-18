namespace Musify.Application.Contracts.Infrastructure
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken);
    }
}