using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.RoutingSlip.Builders;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class UpdateTrackPictureConsumer(
        IBus bus,
        PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdateTrackPictureEvent>
    {
        public const string QueueName = "Update-Track-Picture";

        public async Task Consume(ConsumeContext<UpdateTrackPictureEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}