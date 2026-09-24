using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class UpdateAlbumPictureConsumer(IBus bus, PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<UpdateAlbumPictureEvent>
{
    public Task Consume(ConsumeContext<UpdateAlbumPictureEvent> context) =>
        bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
}
