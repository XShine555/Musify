using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class UpdatePlayListPictureSourceConsumer(IBus bus, PictureSourceRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<UpdatePlayListPictureSourceEvent>
{
    public Task Consume(ConsumeContext<UpdatePlayListPictureSourceEvent> context) =>
        bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
}
