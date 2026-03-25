using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
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
