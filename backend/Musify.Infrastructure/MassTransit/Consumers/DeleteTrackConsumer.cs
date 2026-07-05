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

        public async Task Consume(ConsumeContext<DeleteTrackEvent> consumeContext)
        {
            var routingSlip = (await routingSlipBuilder
                    .BuildAsync(consumeContext.Message.TrackId, consumeContext.Message.UserId, consumeContext.CorrelationId, consumeContext.CancellationToken))
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
