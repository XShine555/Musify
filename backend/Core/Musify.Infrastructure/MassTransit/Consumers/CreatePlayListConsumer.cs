using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class CreatePlayListConsumer(
    IBus bus,
    PlayListPictureSourceRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<CreatePlayListResourcesEvent>
{
    public async Task Consume(ConsumeContext<CreatePlayListResourcesEvent> consumeContext)
    {
        var routingSlip = routingSlipBuilder
            .Build(consumeContext.Message, consumeContext.CorrelationId)
            .Build();

        await bus.Execute(routingSlip);
    }
}
