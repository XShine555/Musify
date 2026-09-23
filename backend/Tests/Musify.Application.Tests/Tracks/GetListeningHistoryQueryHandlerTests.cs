using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetListeningHistoryQueryHandlerTests : HandlerTestBase
    {
        private GetListeningHistoryQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_MultipleListens_ReturnsDistinctTracksMostRecentFirst()
        {
            var owner = TestEntities.User();
            var older = TestEntities.Track(owner, "Older listen");
            var newer = TestEntities.Track(owner, "Newer listen");
            await SeedAsync(
                owner, older, newer,
                TestEntities.ListeningHistory(owner.Id, older.Id, DateTime.UtcNow.AddMinutes(-10)),
                TestEntities.ListeningHistory(owner.Id, older.Id, DateTime.UtcNow.AddMinutes(-5)),
                TestEntities.ListeningHistory(owner.Id, newer.Id, DateTime.UtcNow));

            var result = await CreateHandler().Handle(new GetListeningHistoryQuery(owner.Id), TestContext.Current.CancellationToken);

            Assert.Equal(["Newer listen", "Older listen"], result.Select(track => track.Title));
        }

        [Fact]
        public async Task Handle_NoHistory_ReturnsEmpty()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner);

            var result = await CreateHandler().Handle(new GetListeningHistoryQuery(owner.Id), TestContext.Current.CancellationToken);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_UncountedListens_AreIgnored()
        {
            var owner = TestEntities.User();
            var counted = TestEntities.Track(owner, "Counted");
            var skipped = TestEntities.Track(owner, "Skipped");
            await SeedAsync(
                owner, counted, skipped,
                TestEntities.ListeningHistory(owner.Id, counted.Id, DateTime.UtcNow.AddMinutes(-5), playedSeconds: 90),
                TestEntities.ListeningHistory(owner.Id, skipped.Id, DateTime.UtcNow, playedSeconds: 2, isCounted: false));

            var result = await CreateHandler().Handle(new GetListeningHistoryQuery(owner.Id), TestContext.Current.CancellationToken);

            Assert.Equal(["Counted"], result.Select(track => track.Title));
        }
    }
}
