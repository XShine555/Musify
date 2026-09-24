using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class UpdateAlbumPictureSourceConsumer(
    IBus bus,
    AlbumPictureSourceRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<UpdateAlbumPictureSourceEvent>
{
    public async Task Consume(ConsumeContext<UpdateAlbumPictureSourceEvent> consumeContext)
    {
        var routingSlip = routingSlipBuilder
            .Build(consumeContext.Message, consumeContext.CorrelationId)
            .Build();

        await bus.Execute(routingSlip);
    }
}
