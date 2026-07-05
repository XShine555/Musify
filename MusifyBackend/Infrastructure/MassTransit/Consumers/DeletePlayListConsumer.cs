using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class DeletePlayListConsumer(
        IBus bus,
        DeletePlayListRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<DeletePlayListEvent>
    {
        public const string QueueName = "delete-playList";

        public async Task Consume(ConsumeContext<DeletePlayListEvent> consumeContext)
        {
            var routingSlip = (await routingSlipBuilder
                    .BuildAsync(
                        consumeContext.Message.PlayListId,
                        consumeContext.Message.UserId,
                        consumeContext.CorrelationId,
                        consumeContext.CancellationToken))
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
