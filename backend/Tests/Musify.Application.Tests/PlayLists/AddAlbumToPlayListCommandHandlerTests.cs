using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.PlayLists
{
    public sealed class AddAlbumToPlayListCommandHandlerTests : HandlerTestBase
    {
        private AddAlbumToPlayListCommandHandler CreateHandler() => new(Database, NoOpLogger<AddAlbumToPlayListCommandHandler>());

        [Fact]
        public async Task Handle_AddsAlbumTracksInAlbumOrder()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id);
            var album = TestEntities.Album(owner.Id);
            var first = TestEntities.Track(owner, "First");
            var second = TestEntities.Track(owner, "Second");
            await SeedAsync(
                owner, playList, album, first, second,
                new AlbumHasTrack { AlbumId = album.Id, TrackId = second.Id, TrackNumber = 2 },
                new AlbumHasTrack { AlbumId = album.Id, TrackId = first.Id, TrackNumber = 1 });

            var result = await CreateHandler().Handle(new AddAlbumToPlayListCommand(owner.Id, playList.Id, album.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.AddedCount);
            var links = await Database.PlayListHasTracks.OrderBy(link => link.Position).ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal([first.Id, second.Id], links.Select(link => link.TrackId));
            Assert.Equal([0, 1], links.Select(link => link.Position));
        }

        [Fact]
        public async Task Handle_SkipsTracksAlreadyInPlayList_AndAppendsAfterThem()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id);
            var album = TestEntities.Album(owner.Id);
            var existing = TestEntities.Track(owner, "Existing");
            var fresh = TestEntities.Track(owner, "Fresh");
            await SeedAsync(
                owner, playList, album, existing, fresh,
                new AlbumHasTrack { AlbumId = album.Id, TrackId = existing.Id, TrackNumber = 1 },
                new AlbumHasTrack { AlbumId = album.Id, TrackId = fresh.Id, TrackNumber = 2 },
                new PlayListHasTrack { PlayListId = playList.Id, TrackId = existing.Id, Position = 4 });

            var result = await CreateHandler().Handle(new AddAlbumToPlayListCommand(owner.Id, playList.Id, album.Id), TestContext.Current.CancellationToken);

            Assert.Equal(1, result.Value.AddedCount);
            var link = await Database.PlayListHasTracks.SingleAsync(l => l.TrackId == fresh.Id, TestContext.Current.CancellationToken);
            Assert.Equal(5, link.Position);
        }

        [Fact]
        public async Task Handle_PlayListMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new AddAlbumToPlayListCommand(1, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsUnauthorized()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var playList = TestEntities.PlayList(owner.Id);
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, stranger, playList, album);

            var result = await CreateHandler().Handle(new AddAlbumToPlayListCommand(stranger.Id, playList.Id, album.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id);
            await SeedAsync(owner, playList);

            var result = await CreateHandler().Handle(new AddAlbumToPlayListCommand(owner.Id, playList.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
