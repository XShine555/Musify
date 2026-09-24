using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class DeletePlayListConsumer(IBus bus, DeleteRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<DeletePlayListEvent>
    {
        public async Task Consume(ConsumeContext<DeletePlayListEvent> context)
        {
            var message = context.Message;
            var routingSlip = await routingSlipBuilder.BuildPlayListAsync(
                message.PlayListId, message.UserId, context.CorrelationId, context.CancellationToken);

            await bus.Execute(routingSlip.Build());
        }
    }
}
