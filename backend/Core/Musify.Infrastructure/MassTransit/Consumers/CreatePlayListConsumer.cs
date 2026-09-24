using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class CreatePlayListConsumer(IBus bus, PictureSourceRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<CreatePlayListResourcesEvent>
{
    public Task Consume(ConsumeContext<CreatePlayListResourcesEvent> context) =>
        bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
}
