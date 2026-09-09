using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class AddTrackToAlbumCommandHandlerTests : HandlerTestBase
    {
        private AddTrackToAlbumCommandHandler CreateHandler() => new(Database, NoOpLogger<AddTrackToAlbumCommandHandler>());

        [Fact]
        public async Task Handle_FirstTrack_AddsItAtNumberOne()
        {
            var owner = TestEntities.User();
            var album = TestEntities.UserAlbum(owner.Id);
            var track = TestEntities.LocalTrack(owner);
            await SeedAsync(owner, album, track);

            var result = await CreateHandler().Handle(new AddTrackToAlbumCommand(owner.Id, album.Id, track.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var link = Assert.Single(await Database.AlbumHasTracks.ToListAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, link.TrackNumber);
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            var track = TestEntities.LocalTrack(owner);
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(new AddTrackToAlbumCommand(owner.Id, Guid.NewGuid(), track.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsUnauthorized()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var album = TestEntities.UserAlbum(owner.Id);
            var track = TestEntities.LocalTrack(owner);
            await SeedAsync(owner, stranger, album, track);

            var result = await CreateHandler().Handle(new AddTrackToAlbumCommand(stranger.Id, album.Id, track.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_TrackMissing_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            var album = TestEntities.UserAlbum(owner.Id);
            await SeedAsync(owner, album);

            var result = await CreateHandler().Handle(new AddTrackToAlbumCommand(owner.Id, album.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_TrackOwnedByAnotherUser_ReturnsUnauthorized()
        {
            var owner = TestEntities.User(1, "owner");
            var otherOwner = TestEntities.User(2, "other");
            var album = TestEntities.UserAlbum(owner.Id);
            var foreignTrack = TestEntities.LocalTrack(otherOwner);
            await SeedAsync(owner, otherOwner, album, foreignTrack);

            var result = await CreateHandler().Handle(new AddTrackToAlbumCommand(owner.Id, album.Id, foreignTrack.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_AlreadyInAlbum_ReturnsConflict()
        {
            var owner = TestEntities.User();
            var album = TestEntities.UserAlbum(owner.Id);
            var track = TestEntities.LocalTrack(owner);
            var link = new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 };
            await SeedAsync(owner, album, track, link);

            var result = await CreateHandler().Handle(new AddTrackToAlbumCommand(owner.Id, album.Id, track.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_AlbumAlreadyHasTracks_AppendsAtNextNumber()
        {
            var owner = TestEntities.User();
            var album = TestEntities.UserAlbum(owner.Id);
            var firstTrack = TestEntities.LocalTrack(owner, "First");
            var secondTrack = TestEntities.LocalTrack(owner, "Second");
            var existingLink = new AlbumHasTrack { AlbumId = album.Id, TrackId = firstTrack.Id, TrackNumber = 3 };
            await SeedAsync(owner, album, firstTrack, secondTrack, existingLink);

            var result = await CreateHandler().Handle(new AddTrackToAlbumCommand(owner.Id, album.Id, secondTrack.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var newLink = await Database.AlbumHasTracks.SingleAsync(l => l.TrackId == secondTrack.Id, TestContext.Current.CancellationToken);
            Assert.Equal(4, newLink.TrackNumber);
        }
    }
}
