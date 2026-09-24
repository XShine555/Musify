using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class CreateAlbumWithTracksAndTypeTests : HandlerTestBase
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
        private readonly IStorageService _storageService = Substitute.For<IStorageService>();

        private CreateAlbumCommandHandler CreateHandler()
        {
            _storageService
                .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ObjectMetaData("image/webp", 1024));

            return new CreateAlbumCommandHandler(
                _eventBus,
                Database,
                new UploadIntentValidator(Database, _storageService, TestConfigurations.UploadIntent()),
                NoOpLogger<CreateAlbumCommandHandler>(),
                TestConfigurations.Album());
        }

        private static UploadIntent PictureIntent(long userId) =>
            TestEntities.UploadIntent(userId, UploadIntentPurpose.AlbumPicture, objectName: "cover.webp");

        [Theory]
        [InlineData(AlbumType.Album)]
        [InlineData(AlbumType.EP)]
        [InlineData(AlbumType.Single)]
        public async Task Handle_StoresAndReturnsTheAlbumType(AlbumType type)
        {
            var owner = TestEntities.User();
            var intent = PictureIntent(owner.Id);
            await SeedAsync(owner, intent);

            var result = await CreateHandler().Handle(new CreateAlbumCommand(owner.Id, "Typed", null, null, intent.Id, type), Ct);

            Assert.False(result.IsError);
            Assert.Equal(type, result.Value.Type);
            Assert.Equal(type, (await Database.Albums.AsNoTracking().SingleAsync(Ct)).Type);
        }

        [Fact]
        public async Task Handle_DefaultsToAlbumType()
        {
            var owner = TestEntities.User();
            var intent = PictureIntent(owner.Id);
            await SeedAsync(owner, intent);

            var result = await CreateHandler().Handle(new CreateAlbumCommand(owner.Id, "Default", null, null, intent.Id), Ct);

            Assert.Equal(AlbumType.Album, result.Value.Type);
        }

        [Fact]
        public async Task Handle_WithTrackIds_CreatesAlbumAndLinksTracksInOrderInOneSave()
        {
            var owner = TestEntities.User();
            var first = TestEntities.Track(owner, "First");
            var second = TestEntities.Track(owner, "Second", isPublished: false);
            var third = TestEntities.Track(owner, "Third");
            var intent = PictureIntent(owner.Id);
            await SeedAsync(owner, first, second, third, intent);

            var result = await CreateHandler().Handle(
                new CreateAlbumCommand(owner.Id, "With tracks", null, null, intent.Id, AlbumType.EP, [third.Id, first.Id, second.Id, third.Id]), Ct);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.TrackCount);
            Assert.Equal([third.Id, first.Id], result.Value.CoverTrackIds);

            var links = await Database.AlbumHasTracks.AsNoTracking()
                .Where(link => link.AlbumId == result.Value.Id)
                .OrderBy(link => link.TrackNumber)
                .ToListAsync(Ct);
            Assert.Equal([third.Id, first.Id, second.Id], links.Select(link => link.TrackId));
            Assert.Equal([1, 2, 3], links.Select(link => link.TrackNumber));
            await _eventBus.Received(1).PublishAsync(Arg.Any<CreateAlbumResourcesEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_TrackOfAnotherUser_ReturnsForbiddenAndCreatesNothing()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var theirs = TestEntities.Track(stranger, "Theirs");
            var intent = PictureIntent(owner.Id);
            await SeedAsync(owner, stranger, theirs, intent);

            var result = await CreateHandler().Handle(
                new CreateAlbumCommand(owner.Id, "Stolen", null, null, intent.Id, TrackIds: [theirs.Id]), Ct);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.Empty(await Database.Albums.ToListAsync(Ct));
            Assert.Empty(await Database.AlbumHasTracks.ToListAsync(Ct));
            await _eventBus.DidNotReceive().PublishAsync(Arg.Any<CreateAlbumResourcesEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_MissingTrack_ReturnsNotFoundAndCreatesNothing()
        {
            var owner = TestEntities.User();
            var intent = PictureIntent(owner.Id);
            await SeedAsync(owner, intent);

            var result = await CreateHandler().Handle(
                new CreateAlbumCommand(owner.Id, "Ghost", null, null, intent.Id, TrackIds: [Guid.NewGuid()]), Ct);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
            Assert.Empty(await Database.Albums.ToListAsync(Ct));
        }

        [Fact]
        public async Task Update_ChangesTypeOnlyWhenProvided()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            album.Type = AlbumType.EP;
            await SeedAsync(owner, album);
            var handler = new UpdateAlbumCommandHandler(Database, NoOpLogger<UpdateAlbumCommandHandler>());

            var untouched = await handler.Handle(new UpdateAlbumCommand(owner.Id, album.Id, "Renamed", null, 2024), Ct);
            var changed = await handler.Handle(new UpdateAlbumCommand(owner.Id, album.Id, "Renamed", null, 2024, AlbumType.Single), Ct);

            Assert.Equal(AlbumType.EP, untouched.Value.Type);
            Assert.Equal(AlbumType.Single, changed.Value.Type);
        }
    }
}
