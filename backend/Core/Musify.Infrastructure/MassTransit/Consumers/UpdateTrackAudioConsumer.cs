using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class UpdateTrackAudioConsumer(IBus bus, AudioWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdateTrackAudioEvent>
    {
        public Task Consume(ConsumeContext<UpdateTrackAudioEvent> context) =>
            bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
    }
}
