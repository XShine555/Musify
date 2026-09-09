using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetTracksByUserIdQueryHandlerTests : HandlerTestBase
    {
        private GetTracksByUserIdQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_ReturnsOnlyTracksLinkedToThatUser()
        {
            var owner = TestEntities.User(1, "owner");
            var other = TestEntities.User(2, "other");
            var mine = TestEntities.LocalTrack(owner, "Mine");
            var theirs = TestEntities.LocalTrack(other, "Theirs");
            await SeedAsync(
                owner, other, mine, theirs,
                new UserHasTrack { UserId = owner.Id, TrackId = mine.Id },
                new UserHasTrack { UserId = other.Id, TrackId = theirs.Id });

            var result = await CreateHandler().Handle(new GetTracksByUserIdQuery(owner.Id, Name: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var track = Assert.Single(result.Value.Items);
            Assert.Equal("Mine", track.Title);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        public async Task Handle_NonPositivePageNumber_FallsBackToFirstPage(int requestedPageNumber, int expectedPageNumber)
        {
            var owner = TestEntities.User();
            var track = TestEntities.LocalTrack(owner);
            await SeedAsync(owner, track, new UserHasTrack { UserId = owner.Id, TrackId = track.Id });

            var result = await CreateHandler().Handle(new GetTracksByUserIdQuery(owner.Id, Name: null, PageNumber: requestedPageNumber, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(expectedPageNumber, result.Value.PageNumber);
        }
    }
}
