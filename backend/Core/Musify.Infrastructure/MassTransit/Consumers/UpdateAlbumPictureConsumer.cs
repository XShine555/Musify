using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class UpdateAlbumPictureConsumer(
        IBus bus,
        PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdateAlbumPictureEvent>
    {

        public async Task Consume(ConsumeContext<UpdateAlbumPictureEvent> consumeContext)
        {
            var routingSlip = routingSlipBuilder
                .Build(consumeContext.Message, consumeContext.CorrelationId)
                .Build();

            await bus.Execute(routingSlip);
        }
    }
}
