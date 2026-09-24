using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.PlayLists;
using Musify.Infrastructure.MassTransit.Activities.UploadIntents;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

public class PlayListPictureSourceRoutingSlipBuilder
{
    public RoutingSlipBuilder Build(CreatePlayListResourcesEvent message, Guid? correlationId)
        => BuildSlip(
            message.PlayListId,
            message.PictureIntentId,
            message.Bucket,
            message.PictureSourceKey,
            message.PictureDestinationKey,
            message.Sizes,
            correlationId,
            RoutingSlipVariableNames.ProcessKinds.PlayListCreation);

    public RoutingSlipBuilder Build(UpdatePlayListPictureSourceEvent message, Guid? correlationId)
        => BuildSlip(
            message.PlayListId,
            message.PictureIntentId,
            message.Bucket,
            message.PictureSourceKey,
            message.PictureDestinationKey,
            message.Sizes,
            correlationId,
            RoutingSlipVariableNames.ProcessKinds.PlayListPicture);

    private static RoutingSlipBuilder BuildSlip(
        Guid playListId,
        Guid pictureIntentId,
        string bucket,
        string pictureSourceKey,
        string pictureDestinationKey,
        ImageSizes sizes,
        Guid? correlationId,
        string processKind)
    {
        var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

        routingSlipBuilder.AddSubscription(
            EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
            RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        routingSlipBuilder.AddActivity(
            ActivityNames.CopyPictureToFinal,
            EndpointHelper.BuildExecuteActivityUri(CopyFileInBucketActivity.ExecuteEndpointName),
            new CopyFileInBucketArguments(
                bucket,
                pictureSourceKey,
                bucket,
                pictureDestinationKey));

        routingSlipBuilder.AddActivity(
            ActivityNames.ConsumeUploadIntents,
            EndpointHelper.BuildExecuteActivityUri(ConsumeUploadIntentsActivity.ExecuteEndpointName),
            new ConsumeUploadIntentsArguments([pictureIntentId]));

        routingSlipBuilder.AddActivity(
            ActivityNames.PublishPlayListPictureProcessingEvent,
            EndpointHelper.BuildExecuteActivityUri(PublishPlayListPictureProcessingEventActivity.ExecuteEndpointName),
            new PublishPlayListPictureProcessingEventArguments(
                playListId,
                bucket,
                pictureDestinationKey,
                sizes));

        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.SubjectId, playListId);
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.ProcessKind, processKind);
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.Bucket, bucket);
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.PictureKey, pictureDestinationKey);
        routingSlipBuilder.AddSubscription(
            EndpointHelper.BuildConsumerUri(ProcessingSlipFaultConsumer.QueueName),
            RoutingSlipEvents.Faulted);

        return routingSlipBuilder;
    }
}
