using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.RoutingSlip;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;
using Xunit;

namespace Musify.Infrastructure.Tests.MassTransit
{
    public sealed class RoutingSlipBuilderTests
    {
        private static readonly ImageSizes Sizes = new(new ImageSize("s", 1, 1), new ImageSize("m", 2, 2), new ImageSize("l", 3, 3));

        private static readonly Guid SubjectId = Guid.NewGuid();
        private static readonly Guid IntentId = Guid.NewGuid();
        private static readonly Guid CorrelationId = Guid.NewGuid();

        [Fact]
        public void Create_AlwaysSubscribesTheCleanUpConsumerAndCarriesTheCorrelationId()
        {
            var slip = RoutingSlips.Create(CorrelationId).Build();

            Assert.Equal(CorrelationId, slip.Variables[RoutingSlipVariableNames.Workflow.CorrelationId]);
            var subscription = Assert.Single(slip.Subscriptions);
            Assert.Equal(RoutingSlips.ConsumerUri(RoutingSlipCleanUpConsumer.QueueName), subscription.Address);
            Assert.Equal(RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted, subscription.Events);
        }

        [Fact]
        public void Create_WithoutCorrelationId_UsesEmptyGuid()
        {
            var slip = RoutingSlips.Create(null).Build();

            Assert.Equal(Guid.Empty, slip.Variables[RoutingSlipVariableNames.Workflow.CorrelationId]);
        }

        [Fact]
        public void TrackFaults_AddsSubjectProcessKindAndFaultSubscription()
        {
            var slip = RoutingSlips.Create(null).TrackFaults(SubjectId, "Kind").Build();

            Assert.Equal(SubjectId, slip.Variables[RoutingSlipVariableNames.Workflow.SubjectId]);
            Assert.Equal("Kind", slip.Variables[RoutingSlipVariableNames.Workflow.ProcessKind]);
            Assert.Contains(slip.Subscriptions, subscription =>
                subscription.Address == RoutingSlips.ConsumerUri(ProcessingSlipFaultConsumer.QueueName)
                && subscription.Events == RoutingSlipEvents.Faulted);
        }

        [Fact]
        public void PictureSource_CreateAlbum_CopiesConsumesAndPublishesInOrder()
        {
            var message = new CreateAlbumResourcesEvent(SubjectId, IntentId, "bucket", "source-key", "final-key", Sizes);

            var slip = new PictureSourceRoutingSlipBuilder().Build(message, CorrelationId).Build();

            AssertPictureSourceSlip(slip, RoutingSlipVariableNames.ProcessKinds.AlbumCreation, ActivityNames.PublishAlbumPictureProcessingEvent);
        }

        [Fact]
        public void PictureSource_UpdateAlbum_UsesTheAlbumPictureProcessKind()
        {
            var message = new UpdateAlbumPictureSourceEvent(SubjectId, IntentId, "bucket", "source-key", "final-key", Sizes);

            var slip = new PictureSourceRoutingSlipBuilder().Build(message, CorrelationId).Build();

            AssertPictureSourceSlip(slip, RoutingSlipVariableNames.ProcessKinds.AlbumPicture, ActivityNames.PublishAlbumPictureProcessingEvent);
        }

        [Fact]
        public void PictureSource_CreatePlayList_CopiesConsumesAndPublishesInOrder()
        {
            var message = new CreatePlayListResourcesEvent(SubjectId, IntentId, "bucket", "source-key", "final-key", Sizes);

            var slip = new PictureSourceRoutingSlipBuilder().Build(message, CorrelationId).Build();

            AssertPictureSourceSlip(slip, RoutingSlipVariableNames.ProcessKinds.PlayListCreation, ActivityNames.PublishPlayListPictureProcessingEvent);
        }

        [Fact]
        public void PictureSource_UpdatePlayList_UsesThePlayListPictureProcessKind()
        {
            var message = new UpdatePlayListPictureSourceEvent(SubjectId, IntentId, "bucket", "source-key", "final-key", Sizes);

            var slip = new PictureSourceRoutingSlipBuilder().Build(message, CorrelationId).Build();

            AssertPictureSourceSlip(slip, RoutingSlipVariableNames.ProcessKinds.PlayListPicture, ActivityNames.PublishPlayListPictureProcessingEvent);
        }

        [Fact]
        public void AudioWorkflow_ChainsDownloadTranscodeTransferAndUpdate()
        {
            var configuration = new WorkerConfiguration();
            var message = new UpdateTrackAudioEvent(SubjectId, "source-bucket", "source-key", "destination-bucket", "destination-folder");

            var slip = new AudioWorkflowRoutingSlipBuilder(configuration).Build(message, CorrelationId).Build();

            Assert.Equal(
                [
                    ActivityNames.GenerateAudioWorkflowPaths,
                    ActivityNames.DownloadFile,
                    ActivityNames.TranscodeAudio,
                    ActivityNames.TransferFiles,
                    ActivityNames.UpdateTrackAudio,
                ],
                slip.Itinerary.Select(activity => activity.Name));
            Assert.Equal(RoutingSlipVariableNames.ProcessKinds.TrackAudio, slip.Variables[RoutingSlipVariableNames.Workflow.ProcessKind]);
            Assert.Equal(SubjectId, slip.Variables[RoutingSlipVariableNames.Workflow.SubjectId]);
            Assert.All(slip.Itinerary, activity => Assert.EndsWith("_execute", activity.Address.ToString()));
        }

        private static void AssertPictureSourceSlip(RoutingSlip slip, string processKind, string publishStep)
        {
            Assert.Equal(
                [ActivityNames.CopyPictureToFinal, ActivityNames.ConsumeUploadIntents, publishStep],
                slip.Itinerary.Select(activity => activity.Name));

            Assert.Equal(processKind, slip.Variables[RoutingSlipVariableNames.Workflow.ProcessKind]);
            Assert.Equal(SubjectId, slip.Variables[RoutingSlipVariableNames.Workflow.SubjectId]);
            Assert.Equal("bucket", slip.Variables[RoutingSlipVariableNames.Workflow.Bucket]);
            Assert.Equal("final-key", slip.Variables[RoutingSlipVariableNames.Workflow.PictureKey]);
            Assert.Equal(CorrelationId, slip.Variables[RoutingSlipVariableNames.Workflow.CorrelationId]);

            var copy = new Dictionary<string, object>(slip.Itinerary[0].Arguments, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("source-key", copy["SourceKey"]?.ToString());
            Assert.Equal("final-key", copy["DestinationKey"]?.ToString());
        }
    }
}
