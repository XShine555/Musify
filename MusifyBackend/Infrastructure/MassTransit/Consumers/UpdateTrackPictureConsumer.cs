using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class UpdateTrackPictureConsumer(
        IBus bus,
        PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdateTrackPictureEvent>
    {
        public const string QueueName = "update-track-picture";

        public async Task Consume(ConsumeContext<UpdateTrackPictureEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}