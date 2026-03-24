using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.RoutingSlip.Builders;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class TranscodeAudioFromTrackConsumer(
        IBus bus,
        AudioWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<TranscodeAudioFromTrackEvent>
    {
        public const string QueueName = "transcode-audio-from-track";

        public async Task Consume(ConsumeContext<TranscodeAudioFromTrackEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}