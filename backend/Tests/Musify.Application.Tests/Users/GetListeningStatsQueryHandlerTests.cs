using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Xunit;

namespace Musify.Application.Tests.Users
{
    public sealed class GetListeningStatsQueryHandlerTests : HandlerTestBase
    {
        private GetListeningStatsQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_NoHistory_ReturnsZeroes()
        {
            var user = TestEntities.User();
            await SeedAsync(user);

            var result = await CreateHandler().Handle(new GetListeningStatsQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.Equal(0, result.TracksThisWeek);
            Assert.Equal(0, result.SecondsThisWeek);
            Assert.Equal(0, result.StreakDays);
        }

        [Fact]
        public async Task Handle_ListensThisWeek_CountsDistinctTracksAndTotalSeconds()
        {
            var user = TestEntities.User();
            var trackA = TestEntities.Track(user, "Track A", durationSeconds: 200);
            var trackB = TestEntities.Track(user, "Track B", durationSeconds: 100);
            await SeedAsync(
                user, trackA, trackB,
                TestEntities.ListeningHistory(user.Id, trackA.Id, DateTime.UtcNow.AddDays(-1)),
                TestEntities.ListeningHistory(user.Id, trackA.Id, DateTime.UtcNow.AddHours(-1)),
                TestEntities.ListeningHistory(user.Id, trackB.Id, DateTime.UtcNow),
                TestEntities.ListeningHistory(user.Id, trackB.Id, DateTime.UtcNow.AddDays(-10)));

            var result = await CreateHandler().Handle(new GetListeningStatsQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.Equal(2, result.TracksThisWeek);
            Assert.Equal(500, result.SecondsThisWeek);
        }

        [Fact]
        public async Task Handle_PlayedSeconds_SumsPlayedTimeInsteadOfDuration()
        {
            var user = TestEntities.User();
            var track = TestEntities.Track(user, durationSeconds: 200);
            await SeedAsync(
                user, track,
                TestEntities.ListeningHistory(user.Id, track.Id, playedSeconds: 200),
                TestEntities.ListeningHistory(user.Id, track.Id, playedSeconds: 12, isCounted: false));

            var result = await CreateHandler().Handle(new GetListeningStatsQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.Equal(212, result.SecondsThisWeek);
        }

        [Fact]
        public async Task Handle_UncountedListens_AreExcludedFromTracksAndStreak()
        {
            var user = TestEntities.User();
            var track = TestEntities.Track(user);
            await SeedAsync(
                user, track,
                TestEntities.ListeningHistory(user.Id, track.Id, playedSeconds: 3, isCounted: false));

            var result = await CreateHandler().Handle(new GetListeningStatsQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.Equal(0, result.TracksThisWeek);
            Assert.Equal(0, result.StreakDays);
            Assert.Equal(3, result.SecondsThisWeek);
        }

        [Fact]
        public async Task Handle_ListenedTodayAndYesterday_StreakIsTwo()
        {
            var user = TestEntities.User();
            var track = TestEntities.Track(user);
            await SeedAsync(
                user, track,
                TestEntities.ListeningHistory(user.Id, track.Id, DateTime.UtcNow),
                TestEntities.ListeningHistory(user.Id, track.Id, DateTime.UtcNow.AddDays(-1)));

            var result = await CreateHandler().Handle(new GetListeningStatsQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.Equal(2, result.StreakDays);
        }

        [Fact]
        public async Task Handle_GapBeforeToday_StopsStreakAtGap()
        {
            var user = TestEntities.User();
            var track = TestEntities.Track(user);
            await SeedAsync(
                user, track,
                TestEntities.ListeningHistory(user.Id, track.Id, DateTime.UtcNow),
                TestEntities.ListeningHistory(user.Id, track.Id, DateTime.UtcNow.AddDays(-2)));

            var result = await CreateHandler().Handle(new GetListeningStatsQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.Equal(1, result.StreakDays);
        }

        [Fact]
        public async Task Handle_NoListenToday_StreakIsZero()
        {
            var user = TestEntities.User();
            var track = TestEntities.Track(user);
            await SeedAsync(
                user, track,
                TestEntities.ListeningHistory(user.Id, track.Id, DateTime.UtcNow.AddDays(-1)));

            var result = await CreateHandler().Handle(new GetListeningStatsQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.Equal(0, result.StreakDays);
        }
    }
}
