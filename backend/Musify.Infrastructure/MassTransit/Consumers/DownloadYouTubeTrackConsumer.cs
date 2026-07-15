using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class DownloadYouTubeTrackConsumer(
        IBus bus,
        YouTubeTrackRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<DownloadYouTubeTrackEvent>
    {
        public async Task Consume(ConsumeContext<DownloadYouTubeTrackEvent> consumeContext)
        {
            var audioRoutingSlip = routingSlipBuilder
                .BuildAudioWorkflow(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            var pictureRoutingSlip = routingSlipBuilder
                .BuildPictureWorkflow(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(audioRoutingSlip);
            await bus.Execute(pictureRoutingSlip);
        }
    }
}
