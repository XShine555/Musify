using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.RoutingSlip;

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
            case RoutingSlipVariableNames.ProcessKinds.AlbumPicture:
                await context.Publish(new AlbumPictureProcessingFailed(subjectId));
                break;
            case RoutingSlipVariableNames.ProcessKinds.TrackCreation:
                await context.Publish(new TrackProcessingFailed(
                    subjectId, Variable(variables, RoutingSlipVariableNames.Workflow.Bucket),
                    Variable(variables, RoutingSlipVariableNames.Workflow.PictureKey),
                    Variable(variables, RoutingSlipVariableNames.Workflow.AudioKey)));
                break;
            case RoutingSlipVariableNames.ProcessKinds.AlbumCreation:
                await context.Publish(new AlbumProcessingFailed(
                    subjectId, Variable(variables, RoutingSlipVariableNames.Workflow.Bucket),
                    Variable(variables, RoutingSlipVariableNames.Workflow.PictureKey)));
                break;
            case RoutingSlipVariableNames.ProcessKinds.PlayListCreation:
                await context.Publish(new PlayListProcessingFailed(
                    subjectId, Variable(variables, RoutingSlipVariableNames.Workflow.Bucket),
                    Variable(variables, RoutingSlipVariableNames.Workflow.PictureKey)));
                break;
        }
    }

    private static string? Variable(IDictionary<string, object> variables, string name) =>
        variables.TryGetValue(name, out var value) ? value?.ToString() : null;
}
