using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class UpdatePlayListPictureConsumer(IBus bus, PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<UpdatePlayListPictureEvent>
{
    public Task Consume(ConsumeContext<UpdatePlayListPictureEvent> context) =>
        bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
}
