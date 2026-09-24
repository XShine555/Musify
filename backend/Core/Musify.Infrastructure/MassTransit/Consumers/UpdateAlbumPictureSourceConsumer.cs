using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class UpdateAlbumPictureSourceConsumer(IBus bus, PictureSourceRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdateAlbumPictureSourceEvent>
    {
        public Task Consume(ConsumeContext<UpdateAlbumPictureSourceEvent> context) =>
            bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
    }
}
