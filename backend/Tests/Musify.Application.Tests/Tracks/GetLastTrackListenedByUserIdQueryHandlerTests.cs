using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetLastTrackListenedByUserIdQueryHandlerTests : HandlerTestBase
    {
        private GetLastTrackListenedByUserIdQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_MultipleListens_ReturnsMostRecentlyListenedTrack()
        {
            var owner = TestEntities.User();
            var older = TestEntities.Track(owner, "Older listen");
            var newer = TestEntities.Track(owner, "Newer listen");
            await SeedAsync(
                owner, older, newer,
                TestEntities.ListeningHistory(owner.Id, older.Id, DateTime.UtcNow.AddMinutes(-10)),
                TestEntities.ListeningHistory(owner.Id, newer.Id, DateTime.UtcNow));

            var result = await CreateHandler().Handle(new GetLastTrackListenedByUserIdQuery(owner.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("Newer listen", result.Value.Title);
        }

        [Fact]
        public async Task Handle_NoHistory_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner);

            var result = await CreateHandler().Handle(new GetLastTrackListenedByUserIdQuery(owner.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
