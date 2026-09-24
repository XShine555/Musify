using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class DeleteAlbumConsumer(IBus bus, DeleteRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<DeleteAlbumEvent>
    {
        public async Task Consume(ConsumeContext<DeleteAlbumEvent> context)
        {
            var message = context.Message;
            var routingSlip = await routingSlipBuilder.BuildAlbumAsync(
                message.AlbumId, message.UserId, context.CorrelationId, context.CancellationToken);

            await bus.Execute(routingSlip.Build());
        }
    }
}
