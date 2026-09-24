using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class CreateAlbumConsumer(IBus bus, PictureSourceRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<CreateAlbumResourcesEvent>
    {
        public Task Consume(ConsumeContext<CreateAlbumResourcesEvent> context) =>
            bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
    }
}
