using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class TrackProcessingSlipFaultConsumer : IConsumer<RoutingSlipFaulted>
    {
        public const string QueueName = "track-processing-slip-fault";

        public async Task Consume(ConsumeContext<RoutingSlipFaulted> context)
        {
            var variables = context.Message.Variables;

            if (!variables.TryGetValue(RoutingSlipVariableNames.Workflow.ProcessKind, out var kindObject))
                return;

            if (!variables.TryGetValue(RoutingSlipVariableNames.Workflow.TrackId, out var trackObject)
                || !Guid.TryParse(trackObject?.ToString(), out var trackId))
                return;

            switch (kindObject?.ToString())
            {
                case RoutingSlipVariableNames.ProcessKinds.TrackPicture:
                    await context.Publish(new TrackPictureProcessingFailed(trackId));
                    break;
                case RoutingSlipVariableNames.ProcessKinds.TrackAudio:
                    await context.Publish(new TrackAudioProcessingFailed(trackId));
                    break;
            }
        }
    }
}
