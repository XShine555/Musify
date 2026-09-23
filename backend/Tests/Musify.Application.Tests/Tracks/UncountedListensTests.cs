using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums;
using Musify.Application.Mixes;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class UncountedListensTests : HandlerTestBase
    {
        [Fact]
        public async Task ListeningHistory_IgnoresUncountedListens()
        {
            var owner = TestEntities.User();
            var counted = TestEntities.Track(owner, "Counted");
            var skipped = TestEntities.Track(owner, "Skipped");
            await SeedAsync(
                owner, counted, skipped,
                TestEntities.ListeningHistory(owner.Id, counted.Id, DateTime.UtcNow.AddMinutes(-5), playedSeconds: 90),
                TestEntities.ListeningHistory(owner.Id, skipped.Id, DateTime.UtcNow, playedSeconds: 2, isCounted: false));

            var result = await new GetListeningHistoryQueryHandler(Database)
                .Handle(new GetListeningHistoryQuery(owner.Id), TestContext.Current.CancellationToken);

            Assert.Equal(["Counted"], result.Select(track => track.Title));
        }

        [Fact]
        public async Task RecentlyListenedAlbums_IgnoresUncountedListens()
        {
            var owner = TestEntities.User();
            var countedAlbum = TestEntities.Album(owner.Id, "Counted album");
            var skippedAlbum = TestEntities.Album(owner.Id, "Skipped album");
            var countedTrack = TestEntities.Track(owner, "In counted");
            var skippedTrack = TestEntities.Track(owner, "In skipped");
            await SeedAsync(
                owner, countedAlbum, skippedAlbum, countedTrack, skippedTrack,
                new AlbumHasTrack { AlbumId = countedAlbum.Id, TrackId = countedTrack.Id, TrackNumber = 1 },
                new AlbumHasTrack { AlbumId = skippedAlbum.Id, TrackId = skippedTrack.Id, TrackNumber = 1 },
                TestEntities.ListeningHistory(owner.Id, countedTrack.Id, DateTime.UtcNow.AddDays(-1), playedSeconds: 120),
                TestEntities.ListeningHistory(owner.Id, skippedTrack.Id, DateTime.UtcNow, playedSeconds: 1, isCounted: false));

            var result = await new GetRecentlyListenedAlbumsQueryHandler(Database)
                .Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 10), TestContext.Current.CancellationToken);

            Assert.Equal(["Counted album"], result.Value.Select(album => album.Title));
        }

        [Fact]
        public async Task Mixes_TreatTracksWithOnlyUncountedListensAsUnheard()
        {
            var user = TestEntities.User();
            var skipped = TestEntities.Track(user, "Only skipped");
            await SeedAsync(
                user, skipped,
                new UserHasTrack { UserId = user.Id, TrackId = skipped.Id },
                TestEntities.ListeningHistory(user.Id, skipped.Id, playedSeconds: 3, isCounted: false));

            var handler = new GenerateMixesForUserCommandHandler(
                Database, TestConfigurations.Mix(), NoOpLogger<GenerateMixesForUserCommandHandler>());
            await handler.Handle(new GenerateMixesForUserCommand(user.Id), TestContext.Current.CancellationToken);

            var mixes = await Database.Mixes.ToListAsync(TestContext.Current.CancellationToken);
            Assert.Contains(mixes, mix => mix.Title == "Descubrimiento");
        }

        [Fact]
        public async Task TrackListensCount_OnlyCountsCountedListens()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            await SeedAsync(
                owner, track,
                TestEntities.ListeningHistory(owner.Id, track.Id, playedSeconds: 100),
                TestEntities.ListeningHistory(owner.Id, track.Id, playedSeconds: 100),
                TestEntities.ListeningHistory(owner.Id, track.Id, playedSeconds: 1, isCounted: false));

            var result = await new GetTrackByIdQueryHandler(Database)
                .Handle(new GetTrackByIdQuery(track.Id), TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Value.ListensCount);
        }

        [Fact]
        public async Task Cleanup_DeletesOnlyOldUncountedListens()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            var oldUncounted = TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddDays(-9), playedSeconds: 2, isCounted: false);
            var oldCounted = TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddDays(-9), playedSeconds: 100);
            var recentUncounted = TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddDays(-2), playedSeconds: 2, isCounted: false);
            await SeedAsync(owner, track, oldUncounted, oldCounted, recentUncounted);

            var deleted = await new DeleteStaleUncountedListensCommandHandler(Database, NoOpLogger<DeleteStaleUncountedListensCommandHandler>())
                .Handle(new DeleteStaleUncountedListensCommand(), TestContext.Current.CancellationToken);

            Assert.Equal(1, deleted);
            var remaining = await Database.ListeningHistories.Select(l => l.Id).ToListAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(oldUncounted.Id, remaining);
            Assert.Contains(oldCounted.Id, remaining);
            Assert.Contains(recentUncounted.Id, remaining);
        }
    }
}
