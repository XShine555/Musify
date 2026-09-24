using MassTransit;
using MassTransit.Courier.Contracts;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.RoutingSlip;
using Xunit;

namespace Musify.Infrastructure.Tests.MassTransit
{
    public sealed class ProcessingSlipFaultConsumerTests
    {
        private static readonly Guid SubjectId = Guid.NewGuid();

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task Consume_TrackPicture_PublishesTrackPictureProcessingFailed() =>
            await AssertPublishedAsync<TrackPictureProcessingFailed>(
                RoutingSlipVariableNames.ProcessKinds.TrackPicture, message => message.TrackId == SubjectId);

        [Fact]
        public async Task Consume_TrackAudio_PublishesTrackAudioProcessingFailed() =>
            await AssertPublishedAsync<TrackAudioProcessingFailed>(
                RoutingSlipVariableNames.ProcessKinds.TrackAudio, message => message.TrackId == SubjectId);

        [Fact]
        public async Task Consume_PlayListPicture_PublishesPlayListPictureProcessingFailed() =>
            await AssertPublishedAsync<PlayListPictureProcessingFailed>(
                RoutingSlipVariableNames.ProcessKinds.PlayListPicture, message => message.PlayListId == SubjectId);

        [Fact]
        public async Task Consume_AlbumPicture_PublishesAlbumPictureProcessingFailed() =>
            await AssertPublishedAsync<AlbumPictureProcessingFailed>(
                RoutingSlipVariableNames.ProcessKinds.AlbumPicture, message => message.AlbumId == SubjectId);

        [Fact]
        public async Task Consume_TrackCreation_PublishesTrackProcessingFailedWithKeys() =>
            await AssertPublishedAsync<TrackProcessingFailed>(
                RoutingSlipVariableNames.ProcessKinds.TrackCreation,
                message => message is { Bucket: "bucket", PictureKey: "picture-key", AudioKey: "audio-key" } && message.TrackId == SubjectId);

        [Fact]
        public async Task Consume_AlbumCreation_PublishesAlbumProcessingFailedWithKeys() =>
            await AssertPublishedAsync<AlbumProcessingFailed>(
                RoutingSlipVariableNames.ProcessKinds.AlbumCreation,
                message => message is { Bucket: "bucket", PictureKey: "picture-key" } && message.AlbumId == SubjectId);

        [Fact]
        public async Task Consume_PlayListCreation_PublishesPlayListProcessingFailedWithKeys() =>
            await AssertPublishedAsync<PlayListProcessingFailed>(
                RoutingSlipVariableNames.ProcessKinds.PlayListCreation,
                message => message is { Bucket: "bucket", PictureKey: "picture-key" } && message.PlayListId == SubjectId);

        [Fact]
        public async Task Consume_UnknownKind_PublishesNothing()
        {
            await using var provider = CreateProvider();
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish<RoutingSlipFaulted>(new { Variables = Variables("Something else") }, Ct);

            Assert.True(await harness.Consumed.Any<RoutingSlipFaulted>(Ct));
            Assert.False(await harness.Published.Any<TrackPictureProcessingFailed>(Ct));
            Assert.False(await harness.Published.Any<TrackProcessingFailed>(Ct));
        }

        [Fact]
        public async Task Consume_WithoutProcessKind_PublishesNothing()
        {
            await using var provider = CreateProvider();
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish<RoutingSlipFaulted>(new { Variables = new Dictionary<string, object>() }, Ct);

            Assert.True(await harness.Consumed.Any<RoutingSlipFaulted>(Ct));
            Assert.False(await harness.Published.Any<TrackPictureProcessingFailed>(Ct));
        }

        private static async Task AssertPublishedAsync<TEvent>(string processKind, Func<TEvent, bool> matches)
            where TEvent : class
        {
            await using var provider = CreateProvider();
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            await harness.Bus.Publish<RoutingSlipFaulted>(new { Variables = Variables(processKind) }, Ct);

            Assert.True(await harness.Published.Any<TEvent>(published => matches(published.Context.Message), Ct));
        }

        private static Dictionary<string, object> Variables(string processKind) => new()
        {
            [RoutingSlipVariableNames.Workflow.ProcessKind] = processKind,
            [RoutingSlipVariableNames.Workflow.SubjectId] = SubjectId,
            [RoutingSlipVariableNames.Workflow.Bucket] = "bucket",
            [RoutingSlipVariableNames.Workflow.PictureKey] = "picture-key",
            [RoutingSlipVariableNames.Workflow.AudioKey] = "audio-key",
        };

        private static ServiceProvider CreateProvider() =>
            new ServiceCollection()
                .AddMassTransitTestHarness(bus => bus.AddConsumer<ProcessingSlipFaultConsumer>())
                .BuildServiceProvider(true);
    }
}
