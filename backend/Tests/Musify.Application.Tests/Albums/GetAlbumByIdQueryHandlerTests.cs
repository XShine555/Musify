using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class GetAlbumByIdQueryHandlerTests : HandlerTestBase
    {
        private GetAlbumByIdQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_ExistingAlbum_ReturnsItWithTrackCountAndCovers()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, album, track, new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 });

            var result = await CreateHandler().Handle(new GetAlbumByIdQuery(album.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(album.Id, result.Value.Id);
            Assert.Equal(1, result.Value.TrackCount);
            Assert.Equal([track.Id], result.Value.CoverTrackIds);
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetAlbumByIdQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
