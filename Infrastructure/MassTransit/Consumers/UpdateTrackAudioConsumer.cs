using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class UpdateTrackAudioConsumer(
        IBus bus,
        AudioWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdateTrackAudioEvent>
    {
        public const string QueueName = "update-track-audio";

        public async Task Consume(ConsumeContext<UpdateTrackAudioEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}