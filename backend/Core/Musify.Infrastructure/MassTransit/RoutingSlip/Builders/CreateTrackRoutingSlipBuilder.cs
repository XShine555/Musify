using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Activities.UploadIntents;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class CreateTrackRoutingSlipBuilder
    {
        public RoutingSlipBuilder Build(CreateTrackResourcesEvent message, Guid? correlationId)
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
                    message.Bucket,
                    message.PictureSourceKey,
                    message.Bucket,
                    message.PictureDestinationKey));

            routingSlipBuilder.AddActivity(
                ActivityNames.CopyAudioToFinal,
                EndpointHelper.BuildExecuteActivityUri(CopyFileInBucketActivity.ExecuteEndpointName),
                new CopyFileInBucketArguments(
                    message.Bucket,
                    message.AudioSourceKey,
                    message.Bucket,
                    message.AudioDestinationKey));

            routingSlipBuilder.AddActivity(
                ActivityNames.ConsumeUploadIntents,
                EndpointHelper.BuildExecuteActivityUri(ConsumeUploadIntentsActivity.ExecuteEndpointName),
                new ConsumeUploadIntentsArguments(
                    [message.PictureIntentId, message.AudioIntentId]));

            routingSlipBuilder.AddActivity(
                ActivityNames.PublishTrackProcessingEvents,
                EndpointHelper.BuildExecuteActivityUri(PublishTrackProcessingEventsActivity.ExecuteEndpointName),
                new PublishTrackProcessingEventsArguments(
                    message.TrackId,
                    message.Bucket,
                    message.PictureDestinationKey,
                    message.AudioDestinationKey,
                    message.AudioProcessedFolderKey,
                    message.Small,
                    message.Medium,
                    message.Large));

            return routingSlipBuilder;
        }
    }
}
