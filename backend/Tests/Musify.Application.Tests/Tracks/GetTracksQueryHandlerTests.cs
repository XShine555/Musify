using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetTracksQueryHandlerTests : HandlerTestBase
    {
        private GetTracksQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_NoNameFilter_ReturnsEveryTrackAUserOwns()
        {
            var owner = TestEntities.User();
            var trackA = TestEntities.LocalTrack(owner, "Alpha");
            var trackB = TestEntities.LocalTrack(owner, "Beta");
            await SeedAsync(
                owner, trackA, trackB,
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = trackA.Id },
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = trackB.Id });

            var result = await CreateHandler().Handle(new GetTracksQuery(Name: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.TotalItemCount);
        }

        [Fact]
        public async Task Handle_NameFilter_ReturnsOnlyMatchingTracks()
        {
            var owner = TestEntities.User();
            var match = TestEntities.LocalTrack(owner, "Bohemian Rhapsody");
            var other = TestEntities.LocalTrack(owner, "Imagine");
            await SeedAsync(
                owner, match, other,
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = match.Id },
                new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = other.Id });

            var result = await CreateHandler().Handle(new GetTracksQuery(Name: "rhapsody", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var track = Assert.Single(result.Value.Items);
            Assert.Equal("Bohemian Rhapsody", track.Title);
        }
    }
}
