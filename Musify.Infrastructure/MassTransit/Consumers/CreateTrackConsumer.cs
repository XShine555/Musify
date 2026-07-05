using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class CreateTrackConsumer(
        IBus bus,
        CreateTrackRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<CreateTrackResourcesEvent>
    {
        public const string QueueName = "create-track";

        public async Task Consume(ConsumeContext<CreateTrackResourcesEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
