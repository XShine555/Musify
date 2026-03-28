using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.Activities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class RemoveFileFromBucketRoutingSlipBuilder
    {
        public RoutingSlipBuilder Build(RemoveFileEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddActivity(
                ActivityNames.RemoveFile,
                EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new RemoveFileFromBucketArguments(
                    message.Bucket,
                    message.Key));

            return routingSlipBuilder;
        }
    }
}
