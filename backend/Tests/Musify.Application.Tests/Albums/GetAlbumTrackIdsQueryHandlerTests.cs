using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class GetAlbumTrackIdsQueryHandlerTests : HandlerTestBase
    {
        private GetAlbumTrackIdsQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_Owner_ReturnsAllActiveTrackIdsInOrderBeyondAPage()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            var tracks = Enumerable.Range(1, 120).Select(i => TestEntities.Track(owner, $"T{i}")).ToList();
            var removing = TestEntities.Track(owner, "Removing", lifeCycleStatus: LifeCycleStatus.Removing);
            await SeedAsync([owner, album, removing, .. tracks]);
            var links = tracks.Select((t, i) => new AlbumHasTrack { AlbumId = album.Id, TrackId = t.Id, TrackNumber = i + 1 }).ToList();
            links.Add(new AlbumHasTrack { AlbumId = album.Id, TrackId = removing.Id, TrackNumber = 999 });
            await SeedAsync([.. links]);

            var result = await CreateHandler().Handle(new GetAlbumTrackIdsQuery(owner.Id, album.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(tracks.Select(t => t.Id), result.Value.TrackIds);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, stranger, album);

            var result = await CreateHandler().Handle(new GetAlbumTrackIdsQuery(stranger.Id, album.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_MissingAlbum_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetAlbumTrackIdsQuery(1, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
