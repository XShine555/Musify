using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetTracksByUserIdStatusAndAlbumsTests : HandlerTestBase
    {
        private GetTracksByUserIdQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_StatusFilter_ReturnsOnlyMatchingStatuses()
        {
            var owner = TestEntities.User();
            var done = TestEntities.Track(owner, "Done");
            var pending = TestEntities.Track(owner, "Pending", audio: TestEntities.PendingAudio());
            var failed = TestEntities.Track(owner, "Failed", audio: new TrackAudio { TranscodeStatus = ProcessingStatus.Failed });
            await SeedAsync(owner, done, pending, failed);

            var result = await CreateHandler().Handle(
                new GetTracksByUserIdQuery(owner.Id, null, 1, 10, [ProcessingStatus.Pending, ProcessingStatus.Failed]),
                TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(["Failed", "Pending"], result.Value.Items.Select(t => t.Title).Order().ToList());
            Assert.Equal(2, result.Value.TotalItemCount);
        }

        [Fact]
        public async Task Handle_NoStatusFilter_ReturnsEveryStatus()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.Track(owner, "A"), TestEntities.Track(owner, "B", audio: TestEntities.PendingAudio()));

            var result = await CreateHandler().Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 10), TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Value.TotalItemCount);
        }

        [Fact]
        public async Task Handle_ListsActiveAlbumsOfEachTrack()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner, "Song");
            var loose = TestEntities.Track(owner, "Loose");
            var first = TestEntities.Album(owner.Id, "First");
            var second = TestEntities.Album(owner.Id, "Second");
            var removed = TestEntities.Album(owner.Id, "Gone");
            removed.LifeCycleStatus = LifeCycleStatus.Removing;
            await SeedAsync(
                owner, track, loose, first, second, removed,
                new AlbumHasTrack { AlbumId = first.Id, TrackId = track.Id },
                new AlbumHasTrack { AlbumId = second.Id, TrackId = track.Id },
                new AlbumHasTrack { AlbumId = removed.Id, TrackId = track.Id });

            var result = await CreateHandler().Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 10), TestContext.Current.CancellationToken);

            var song = result.Value.Items.Single(t => t.Title == "Song");
            Assert.Equal(["First", "Second"], song.Albums.Select(a => a.Title).Order().ToList());
            Assert.Contains(song.Albums, a => a.Id == first.Id);
            Assert.Empty(result.Value.Items.Single(t => t.Title == "Loose").Albums);
        }

        [Fact]
        public async Task Handle_Paginates()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.Track(owner, "A"), TestEntities.Track(owner, "B"), TestEntities.Track(owner, "C"));

            var result = await CreateHandler().Handle(new GetTracksByUserIdQuery(owner.Id, null, 2, 2), TestContext.Current.CancellationToken);

            Assert.Single(result.Value.Items);
            Assert.Equal(3, result.Value.TotalItemCount);
            Assert.True(result.Value.HasPreviousPage);
        }
    }
}
