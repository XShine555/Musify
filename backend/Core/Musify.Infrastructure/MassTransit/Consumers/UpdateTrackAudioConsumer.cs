using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class UpdateTrackAudioConsumer(
    IBus bus,
    AudioWorkflowRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<UpdateTrackAudioEvent>
{
    public async Task Consume(ConsumeContext<UpdateTrackAudioEvent> consumeContext)
    {
        var routingSlip = routingSlipBuilder
            .Build(consumeContext.Message, consumeContext.CorrelationId)
            .Build();

        await bus.Execute(routingSlip);
    }
}
