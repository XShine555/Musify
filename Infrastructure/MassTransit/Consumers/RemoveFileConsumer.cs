using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.RoutingSlip.Builders;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class RemoveFileConsumer(
        IBus bus,
        RemoveFileFromBucketRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<RemoveFileEvent>
    {
        public const string QueueName = "remove-file";

        public async Task Consume(ConsumeContext<RemoveFileEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
