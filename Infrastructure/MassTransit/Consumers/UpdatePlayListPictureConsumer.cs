using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.RoutingSlip.Builders;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class UpdatePlayListPictureConsumer(
        IBus bus,
        PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdatePlayListPictureEvent>
    {
        public const string QueueName = "update-playList-picture";

        public async Task Consume(ConsumeContext<UpdatePlayListPictureEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}