namespace Musify.Application.Abstractions.Infrastructure
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken);
    }
}