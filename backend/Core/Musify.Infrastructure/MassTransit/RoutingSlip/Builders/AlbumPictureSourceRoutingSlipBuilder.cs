using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Infrastructure.MassTransit.Activities.Albums;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.UploadIntents;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

public class AlbumPictureSourceRoutingSlipBuilder
{
    public RoutingSlipBuilder Build(CreateAlbumResourcesEvent message, Guid? correlationId)
        => BuildSlip(
            message.AlbumId,
            message.PictureIntentId,
            message.Bucket,
            message.PictureSourceKey,
            message.PictureDestinationKey,
            message.Small,
            message.Medium,
            message.Large,
            correlationId,
            RoutingSlipVariableNames.ProcessKinds.AlbumCreation);

    public RoutingSlipBuilder Build(UpdateAlbumPictureSourceEvent message, Guid? correlationId)
        => BuildSlip(
            message.AlbumId,
            message.PictureIntentId,
            message.Bucket,
            message.PictureSourceKey,
            message.PictureDestinationKey,
            message.Small,
            message.Medium,
            message.Large,
            correlationId,
            RoutingSlipVariableNames.ProcessKinds.AlbumPicture);

    private static RoutingSlipBuilder BuildSlip(
        Guid albumId,
        Guid pictureIntentId,
        string bucket,
        string pictureSourceKey,
        string pictureDestinationKey,
        ImageSize small,
        ImageSize medium,
        ImageSize large,
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
            ActivityNames.PublishAlbumPictureProcessingEvent,
            EndpointHelper.BuildExecuteActivityUri(PublishAlbumPictureProcessingEventActivity.ExecuteEndpointName),
            new PublishAlbumPictureProcessingEventArguments(
                albumId,
                bucket,
                pictureDestinationKey,
                small,
                medium,
                large));

        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.SubjectId, albumId);
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.ProcessKind, processKind);
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.Bucket, bucket);
        routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.PictureKey, pictureDestinationKey);
        routingSlipBuilder.AddSubscription(
            EndpointHelper.BuildConsumerUri(ProcessingSlipFaultConsumer.QueueName),
            RoutingSlipEvents.Faulted);

        return routingSlipBuilder;
    }
}
