using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class DeleteTrackRoutingSlipBuilder
    {
        public RoutingSlipBuilder Build(Guid trackId, Guid userId, Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddActivity(
                ActivityNames.DeleteTrack,
                EndpointHelper.BuildExecuteActivityUri(DeleteTrackActivity.ExecuteEndpointName),
                new DeleteTrackArguments(trackId, userId));

            return routingSlipBuilder;
        }
    }
}
