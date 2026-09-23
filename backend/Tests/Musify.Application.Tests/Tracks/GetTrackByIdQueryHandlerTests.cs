using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Application.Tracks.Responses;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetTrackByIdQueryHandlerTests : HandlerTestBase
    {
        private GetTrackByIdQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_LocalTrack_ReturnsItWithOwnerAsArtist()
        {
            var owner = TestEntities.User(1, "artist-name");
            var track = TestEntities.Track(owner, "My Song");
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(new GetTrackByIdQuery(track.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("My Song", result.Value.Title);
            Assert.Equal(owner.Name, result.Value.Artist);
            var localResponse = Assert.IsType<TrackApplicationResponse>(result.Value);
            Assert.Equal(owner.Id, localResponse.OwnerUserId);
        }

        [Fact]
        public async Task Handle_TrackMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetTrackByIdQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_ListensCount_OnlyCountsCountedListens()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            await SeedAsync(
                owner, track,
                TestEntities.ListeningHistory(owner.Id, track.Id, playedSeconds: 100),
                TestEntities.ListeningHistory(owner.Id, track.Id, playedSeconds: 100),
                TestEntities.ListeningHistory(owner.Id, track.Id, playedSeconds: 1, isCounted: false));

            var result = await CreateHandler().Handle(new GetTrackByIdQuery(track.Id), TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Value.ListensCount);
        }
    }
}
