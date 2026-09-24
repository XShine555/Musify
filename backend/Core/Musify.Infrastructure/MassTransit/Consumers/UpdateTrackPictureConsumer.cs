using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class UpdateTrackPictureConsumer(IBus bus, PictureWorkflowRoutingSlipBuilder routingSlipBuilder)
        : IConsumer<UpdateTrackPictureEvent>
    {
        public Task Consume(ConsumeContext<UpdateTrackPictureEvent> context) =>
            bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
    }
}
