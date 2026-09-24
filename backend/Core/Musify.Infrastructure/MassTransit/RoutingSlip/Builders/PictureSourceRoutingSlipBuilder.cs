using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.Albums;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.PlayLists;
using Musify.Infrastructure.MassTransit.Activities.UploadIntents;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    /// <summary>
    /// Moves an uploaded album/playlist picture to its final key, consumes the upload intent and hands over to the picture workflow.
    /// Creation faults mark the whole entity as failed; update faults only report the picture as failed.
    /// </summary>
    public class PictureSourceRoutingSlipBuilder
    {
        public RoutingSlipBuilder Build(CreateAlbumResourcesEvent message, Guid? correlationId) =>
            BuildSlip(
                message.AlbumId, message.PictureIntentId, message.Bucket, message.PictureSourceKey, message.PictureDestinationKey,
                correlationId, RoutingSlipVariableNames.ProcessKinds.AlbumCreation,
                ActivityNames.PublishAlbumPictureProcessingEvent, PublishAlbumPictureProcessingEventActivity.ExecuteEndpointName,
                new PublishAlbumPictureProcessingEventArguments(message.AlbumId, message.Bucket, message.PictureDestinationKey, message.Sizes));

        public RoutingSlipBuilder Build(UpdateAlbumPictureSourceEvent message, Guid? correlationId) =>
            BuildSlip(
                message.AlbumId, message.PictureIntentId, message.Bucket, message.PictureSourceKey, message.PictureDestinationKey,
                correlationId, RoutingSlipVariableNames.ProcessKinds.AlbumPicture,
                ActivityNames.PublishAlbumPictureProcessingEvent, PublishAlbumPictureProcessingEventActivity.ExecuteEndpointName,
                new PublishAlbumPictureProcessingEventArguments(message.AlbumId, message.Bucket, message.PictureDestinationKey, message.Sizes));

        public RoutingSlipBuilder Build(CreatePlayListResourcesEvent message, Guid? correlationId) =>
            BuildSlip(
                message.PlayListId, message.PictureIntentId, message.Bucket, message.PictureSourceKey, message.PictureDestinationKey,
                correlationId, RoutingSlipVariableNames.ProcessKinds.PlayListCreation,
                ActivityNames.PublishPlayListPictureProcessingEvent, PublishPlayListPictureProcessingEventActivity.ExecuteEndpointName,
                new PublishPlayListPictureProcessingEventArguments(message.PlayListId, message.Bucket, message.PictureDestinationKey, message.Sizes));

        public RoutingSlipBuilder Build(UpdatePlayListPictureSourceEvent message, Guid? correlationId) =>
            BuildSlip(
                message.PlayListId, message.PictureIntentId, message.Bucket, message.PictureSourceKey, message.PictureDestinationKey,
                correlationId, RoutingSlipVariableNames.ProcessKinds.PlayListPicture,
                ActivityNames.PublishPlayListPictureProcessingEvent, PublishPlayListPictureProcessingEventActivity.ExecuteEndpointName,
                new PublishPlayListPictureProcessingEventArguments(message.PlayListId, message.Bucket, message.PictureDestinationKey, message.Sizes));

        private static RoutingSlipBuilder BuildSlip(
            Guid subjectId,
            Guid pictureIntentId,
            string bucket,
            string pictureSourceKey,
            string pictureDestinationKey,
            Guid? correlationId,
            string processKind,
            string publishStepName,
            string publishEndpointName,
            object publishArguments)
        {
            var builder = RoutingSlips.Create(correlationId)
                .AddStep(
                    ActivityNames.CopyPictureToFinal,
                    CopyFileInBucketActivity.ExecuteEndpointName,
                    new CopyFileInBucketArguments(bucket, pictureSourceKey, bucket, pictureDestinationKey))
                .AddStep(
                    ActivityNames.ConsumeUploadIntents,
                    ConsumeUploadIntentsActivity.ExecuteEndpointName,
                    new ConsumeUploadIntentsArguments([pictureIntentId]))
                .AddStep(publishStepName, publishEndpointName, publishArguments)
                .TrackFaults(subjectId, processKind);

            builder.AddVariable(RoutingSlipVariableNames.Workflow.Bucket, bucket);
            builder.AddVariable(RoutingSlipVariableNames.Workflow.PictureKey, pictureDestinationKey);

            return builder;
        }
    }
}
