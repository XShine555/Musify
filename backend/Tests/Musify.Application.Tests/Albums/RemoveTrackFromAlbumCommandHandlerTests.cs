using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class RemoveTrackFromAlbumCommandHandlerTests : HandlerTestBase
    {
        private RemoveTrackFromAlbumCommandHandler CreateHandler() =>
            new(Database, NoOpLogger<RemoveTrackFromAlbumCommandHandler>());

        [Fact]
        public async Task Handle_TrackInAlbum_RemovesItAndRenumbersTheRest()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            var first = TestEntities.Track(owner, "First");
            var second = TestEntities.Track(owner, "Second");
            var third = TestEntities.Track(owner, "Third");
            await SeedAsync(
                owner, album, first, second, third,
                new AlbumHasTrack { AlbumId = album.Id, TrackId = first.Id, TrackNumber = 1 },
                new AlbumHasTrack { AlbumId = album.Id, TrackId = second.Id, TrackNumber = 2 },
                new AlbumHasTrack { AlbumId = album.Id, TrackId = third.Id, TrackNumber = 3 });

            var result = await CreateHandler().Handle(new RemoveTrackFromAlbumCommand(owner.Id, album.Id, second.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var remaining = await Database.AlbumHasTracks
                .Where(link => link.AlbumId == album.Id)
                .OrderBy(link => link.TrackNumber)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal([first.Id, third.Id], remaining.Select(link => link.TrackId));
            Assert.Equal([1, 2], remaining.Select(link => link.TrackNumber));
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new RemoveTrackFromAlbumCommand(1, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, stranger, album);

            var result = await CreateHandler().Handle(new RemoveTrackFromAlbumCommand(stranger.Id, album.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_TrackNotInAlbum_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, album);

            var result = await CreateHandler().Handle(new RemoveTrackFromAlbumCommand(owner.Id, album.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
