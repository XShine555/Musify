using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class UpdatePlayListPictureSourceConsumer(
        IBus bus,
        PlayListPictureSourceRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdatePlayListPictureSourceEvent>
    {
        public const string QueueName = "update-playList-picture-source";

        public async Task Consume(ConsumeContext<UpdatePlayListPictureSourceEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
