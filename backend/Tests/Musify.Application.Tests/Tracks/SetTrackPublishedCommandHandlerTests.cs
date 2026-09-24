using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class SetTrackPublishedCommandHandlerTests : HandlerTestBase
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private SetTrackPublishedCommandHandler CreateSingleHandler() => new(Database, NoOpLogger<SetTrackPublishedCommandHandler>());

        private SetTracksPublishedCommandHandler CreateBatchHandler() => new(Database, NoOpLogger<SetTracksPublishedCommandHandler>());

        private async Task<bool> IsPublishedAsync(Guid trackId) =>
            (await Database.Tracks.AsNoTracking().SingleAsync(t => t.Id == trackId, Ct)).IsPublished;

        [Fact]
        public async Task Single_ProcessedTrack_CanBePublishedAndUnpublished()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner, isPublished: false);
            await SeedAsync(owner, track);

            var publish = await CreateSingleHandler().Handle(new SetTrackPublishedCommand(owner.Id, track.Id, true), Ct);
            var published = await IsPublishedAsync(track.Id);
            var unpublish = await CreateSingleHandler().Handle(new SetTrackPublishedCommand(owner.Id, track.Id, false), Ct);

            Assert.False(publish.IsError);
            Assert.True(published);
            Assert.False(unpublish.IsError);
            Assert.False(await IsPublishedAsync(track.Id));
        }

        [Theory]
        [InlineData(ProcessingStatus.Pending)]
        [InlineData(ProcessingStatus.Processing)]
        [InlineData(ProcessingStatus.Failed)]
        public async Task Single_AudioNotCompleted_CannotBePublished(ProcessingStatus status)
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner, audio: new Domain.ValueObjects.TrackAudio { TranscodeStatus = status }, isPublished: false);
            await SeedAsync(owner, track);

            var result = await CreateSingleHandler().Handle(new SetTrackPublishedCommand(owner.Id, track.Id, true), Ct);

            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
            Assert.False(await IsPublishedAsync(track.Id));
        }

        [Fact]
        public async Task Single_UnprocessedTrackThatIsPublished_CanStillBeUnpublished()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner, audio: TestEntities.PendingAudio(), isPublished: true);
            await SeedAsync(owner, track);

            var result = await CreateSingleHandler().Handle(new SetTrackPublishedCommand(owner.Id, track.Id, false), Ct);

            Assert.False(result.IsError);
            Assert.False(await IsPublishedAsync(track.Id));
        }

        [Fact]
        public async Task Single_NotTheOwner_ReturnsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var track = TestEntities.Track(owner, isPublished: false);
            await SeedAsync(owner, stranger, track);

            var result = await CreateSingleHandler().Handle(new SetTrackPublishedCommand(stranger.Id, track.Id, true), Ct);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.False(await IsPublishedAsync(track.Id));
        }

        [Fact]
        public async Task Single_MissingOrRemovingTrack_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            var removing = TestEntities.Track(owner, lifeCycleStatus: LifeCycleStatus.Removing, isPublished: false);
            await SeedAsync(owner, removing);

            var missing = await CreateSingleHandler().Handle(new SetTrackPublishedCommand(owner.Id, Guid.NewGuid(), true), Ct);
            var gone = await CreateSingleHandler().Handle(new SetTrackPublishedCommand(owner.Id, removing.Id, true), Ct);

            Assert.Equal(ErrorType.NotFound, missing.FirstError.Type);
            Assert.Equal(ErrorType.NotFound, gone.FirstError.Type);
        }

        [Fact]
        public async Task Batch_PublishesProcessedTracksAndSkipsTheRest()
        {
            var owner = TestEntities.User();
            var ready = TestEntities.Track(owner, "Ready", isPublished: false);
            var processing = TestEntities.Track(owner, "Processing", audio: TestEntities.PendingAudio(), isPublished: false);
            var missing = Guid.NewGuid();
            await SeedAsync(owner, ready, processing);

            var result = await CreateBatchHandler().Handle(
                new SetTracksPublishedCommand(owner.Id, [ready.Id, processing.Id, missing, ready.Id], true), Ct);

            Assert.False(result.IsError);
            Assert.Equal([ready.Id], result.Value.UpdatedTrackIds);
            Assert.Equal([processing.Id, missing], result.Value.SkippedTrackIds);
            Assert.True(await IsPublishedAsync(ready.Id));
            Assert.False(await IsPublishedAsync(processing.Id));
        }

        [Fact]
        public async Task Batch_Unpublish_AppliesToEveryOwnedTrack()
        {
            var owner = TestEntities.User();
            var first = TestEntities.Track(owner, "First");
            var second = TestEntities.Track(owner, "Second", audio: TestEntities.PendingAudio());
            await SeedAsync(owner, first, second);

            var result = await CreateBatchHandler().Handle(new SetTracksPublishedCommand(owner.Id, [first.Id, second.Id], false), Ct);

            Assert.Equal(2, result.Value.UpdatedTrackIds.Count);
            Assert.Empty(result.Value.SkippedTrackIds);
            Assert.False(await IsPublishedAsync(first.Id));
            Assert.False(await IsPublishedAsync(second.Id));
        }

        [Fact]
        public async Task Batch_TrackOfAnotherUser_ReturnsForbiddenAndChangesNothing()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var mine = TestEntities.Track(owner, "Mine", isPublished: false);
            var theirs = TestEntities.Track(stranger, "Theirs", isPublished: false);
            await SeedAsync(owner, stranger, mine, theirs);

            var result = await CreateBatchHandler().Handle(new SetTracksPublishedCommand(owner.Id, [mine.Id, theirs.Id], true), Ct);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.False(await IsPublishedAsync(mine.Id));
            Assert.False(await IsPublishedAsync(theirs.Id));
        }
    }
}
