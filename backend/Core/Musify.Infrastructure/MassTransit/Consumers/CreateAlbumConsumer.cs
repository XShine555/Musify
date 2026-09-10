using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class CreateAlbumConsumer(
        IBus bus,
        AlbumPictureSourceRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<CreateAlbumResourcesEvent>
    {

        public async Task Consume(ConsumeContext<CreateAlbumResourcesEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
