using MassTransit;
using Musify.Application.Abstractions.Infrastructure;

namespace Musify.Infrastructure.MassTransit
{
    public class MassTransitEventBus(IPublishEndpoint publishEndpoint)
        : IEventBus
    {
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            await publishEndpoint.Publish(message, cancellationToken);
        }
    }
}