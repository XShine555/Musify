using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class CreateTrackConsumer(IBus bus, CreateTrackRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<CreateTrackResourcesEvent>
{
    public Task Consume(ConsumeContext<CreateTrackResourcesEvent> context) =>
        bus.Execute(routingSlipBuilder.Build(context.Message, context.CorrelationId).Build());
}
