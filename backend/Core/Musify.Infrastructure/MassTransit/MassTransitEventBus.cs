using MassTransit;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.MassTransit;

public class MassTransitEventBus(IPublishEndpoint publishEndpoint)
    : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class =>
        publishEndpoint.Publish(message, cancellationToken);
}
