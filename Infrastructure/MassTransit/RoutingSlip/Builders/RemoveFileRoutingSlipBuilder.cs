using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Consumers;

namespace Musify.Infrastructure.Messaging.RoutingSlip.Builders
{
    public class RemoveFileRoutingSlipBuilder
    {
        public RoutingSlipBuilder Build(RemoveFileEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddActivity(
                ActivityNames.RemoveFile,
                MessagingHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new Activities.Arguments.RemoveFileFromBucketArguments(
                    message.BucketName,
                    message.KeyName));

            return routingSlipBuilder;
        }
    }
}
