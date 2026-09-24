using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class DeleteTrackConsumer(IBus bus, DeleteRoutingSlipBuilder routingSlipBuilder)
    : IConsumer<DeleteTrackEvent>
{
    public async Task Consume(ConsumeContext<DeleteTrackEvent> context)
    {
        var message = context.Message;
        var routingSlip = await routingSlipBuilder.BuildTrackAsync(
            message.TrackId, message.UserId, context.CorrelationId, context.CancellationToken);

        await bus.Execute(routingSlip.Build());
    }
}
