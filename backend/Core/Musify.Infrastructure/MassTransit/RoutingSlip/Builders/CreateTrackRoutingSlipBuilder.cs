using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Activities.UploadIntents;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class CreateTrackRoutingSlipBuilder
    {
        public RoutingSlipBuilder Build(CreateTrackResourcesEvent message, Guid? correlationId)
        {
            var builder = RoutingSlips.Create(correlationId)
                .AddStep(
                    ActivityNames.CopyPictureToFinal,
                    CopyFileInBucketActivity.ExecuteEndpointName,
                    new CopyFileInBucketArguments(message.Bucket, message.PictureSourceKey, message.Bucket, message.PictureDestinationKey))
                .AddStep(
                    ActivityNames.CopyAudioToFinal,
                    CopyFileInBucketActivity.ExecuteEndpointName,
                    new CopyFileInBucketArguments(message.Bucket, message.AudioSourceKey, message.Bucket, message.AudioDestinationKey))
                .AddStep(
                    ActivityNames.ConsumeUploadIntents,
                    ConsumeUploadIntentsActivity.ExecuteEndpointName,
                    new ConsumeUploadIntentsArguments([message.PictureIntentId, message.AudioIntentId]))
                .AddStep(
                    ActivityNames.PublishTrackProcessingEvents,
                    PublishTrackProcessingEventsActivity.ExecuteEndpointName,
                    new PublishTrackProcessingEventsArguments(
                        message.TrackId,
                        message.Bucket,
                        message.PictureDestinationKey,
                        message.AudioDestinationKey,
                        message.AudioProcessedFolderKey,
                        message.Sizes))
                .TrackFaults(message.TrackId, RoutingSlipVariableNames.ProcessKinds.TrackCreation);

            builder.AddVariable(RoutingSlipVariableNames.Workflow.Bucket, message.Bucket);
            builder.AddVariable(RoutingSlipVariableNames.Workflow.PictureKey, message.PictureDestinationKey);
            builder.AddVariable(RoutingSlipVariableNames.Workflow.AudioKey, message.AudioDestinationKey);

            return builder;
        }
    }
}
