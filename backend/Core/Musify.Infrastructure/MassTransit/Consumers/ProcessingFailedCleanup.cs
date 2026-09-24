using MassTransit;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    internal static class ProcessingFailedCleanup
    {
        /// <summary>Removes whatever files were left behind and marks the entity as failed.</summary>
        public static Task ExecuteAsync(
            IBus bus,
            Guid subjectId,
            string markStepName,
            string markEndpointName,
            string? bucket,
            params (string StepName, string? Key)[] files)
        {
            var builder = RoutingSlips.Create(correlationId: null);

            if (!string.IsNullOrWhiteSpace(bucket))
            {
                foreach (var (stepName, key) in files)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        builder.AddRemoveFile(stepName, bucket, key);
                }
            }

            builder.AddStep(markStepName, markEndpointName, new MarkLifeCycleArguments(subjectId, LifeCycleStatus.Failed));

            return bus.Execute(builder.Build());
        }
    }
}
