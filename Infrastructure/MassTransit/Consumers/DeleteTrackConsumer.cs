using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class DeleteTrackConsumer(
        IBus bus,
        DeleteTrackRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<DeleteTrackEvent>
    {
        public const string QueueName = "delete-track";

        public async Task Consume(ConsumeContext<DeleteTrackEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message.TrackId, consumeContext.Message.UserId, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
