using MassTransit;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.MassTransit
{
    public class MassTransitEventBus(IPublishEndpoint publishEndpoint)
        : IEventBus
    {
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            await publishEndpoint.Publish(message, cancellationToken);
        }
    }
}