namespace Musify.Application.Contracts
{
    public interface IEventBus
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class;
    }
}
