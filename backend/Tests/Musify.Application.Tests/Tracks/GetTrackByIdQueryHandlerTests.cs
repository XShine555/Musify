using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
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
            var track = TestEntities.LocalTrack(owner, "My Song");
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(new GetTrackByIdQuery(track.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("My Song", result.Value.Title);
            Assert.Equal(owner.Name, result.Value.Artist);
            Assert.Equal(owner.Id, result.Value.OwnerUserId);
        }

        [Fact]
        public async Task Handle_TrackMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetTrackByIdQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
