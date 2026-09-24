using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class ProcessingSlipFaultConsumer : IConsumer<RoutingSlipFaulted>
{
    public const string QueueName = "processing-slip-fault";

    public async Task Consume(ConsumeContext<RoutingSlipFaulted> context)
    {
        var variables = context.Message.Variables;

        if (!variables.TryGetValue(RoutingSlipVariableNames.Workflow.ProcessKind, out var kindObject))
            return;

        if (!variables.TryGetValue(RoutingSlipVariableNames.Workflow.SubjectId, out var subjectObject)
            || !Guid.TryParse(subjectObject?.ToString(), out var subjectId))
        {
            return;
        }

        switch (kindObject?.ToString())
        {
            case RoutingSlipVariableNames.ProcessKinds.TrackPicture:
                await context.Publish(new TrackPictureProcessingFailed(subjectId));
                break;
            case RoutingSlipVariableNames.ProcessKinds.TrackAudio:
                await context.Publish(new TrackAudioProcessingFailed(subjectId));
                break;
            case RoutingSlipVariableNames.ProcessKinds.PlayListPicture:
                await context.Publish(new PlayListPictureProcessingFailed(subjectId));
                break;
        }
    }
}
