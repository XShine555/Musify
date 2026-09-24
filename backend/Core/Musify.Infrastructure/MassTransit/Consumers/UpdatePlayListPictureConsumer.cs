using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class UpdatePlayListPictureConsumer(
    IBus bus,
    PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<UpdatePlayListPictureEvent>
{
    public async Task Consume(ConsumeContext<UpdatePlayListPictureEvent> consumeContext)
    {
        var routingSlip = routingSlipBuilder
            .Build(consumeContext.Message, consumeContext.CorrelationId)
            .Build();

        await bus.Execute(routingSlip);
    }
}
