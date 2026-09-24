using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class DeleteAlbumConsumer(
    IBus bus,
    DeleteAlbumRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<DeleteAlbumEvent>
{
    public async Task Consume(ConsumeContext<DeleteAlbumEvent> consumeContext)
    {
        var routingSlip = (await routingSlipBuilder
                .BuildAsync(
                    consumeContext.Message.AlbumId,
                    consumeContext.Message.UserId,
                    consumeContext.CorrelationId,
                    consumeContext.CancellationToken))
            .Build();

        await bus.Execute(routingSlip);
    }
}
