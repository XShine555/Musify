using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip
{
    internal static class RoutingSlips
    {
        public static Uri ExecuteUri(string endpointName) => new($"queue:{endpointName}_execute");

        public static Uri ConsumerUri(string queueName) => new($"queue:{queueName}");

        public static RoutingSlipBuilder Create(Guid? correlationId)
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);
            builder.AddSubscription(
                ConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            return builder;
        }

        public static RoutingSlipBuilder AddStep(
            this RoutingSlipBuilder builder, string name, string executeEndpointName, object arguments)
        {
            builder.AddActivity(name, ExecuteUri(executeEndpointName), arguments);
            return builder;
        }

        public static RoutingSlipBuilder TrackFaults(
            this RoutingSlipBuilder builder, Guid subjectId, string processKind)
        {
            builder.AddVariable(RoutingSlipVariableNames.Workflow.SubjectId, subjectId);
            builder.AddVariable(RoutingSlipVariableNames.Workflow.ProcessKind, processKind);
            builder.AddSubscription(ConsumerUri(ProcessingSlipFaultConsumer.QueueName), RoutingSlipEvents.Faulted);
            return builder;
        }

        public static RoutingSlipBuilder AddRemoveFile(
            this RoutingSlipBuilder builder, string stepName, string bucket, string key) =>
            builder.AddStep(stepName, RemoveFileFromBucketActivity.ExecuteEndpointName, new RemoveFileFromBucketArguments(bucket, key));
    }
}
