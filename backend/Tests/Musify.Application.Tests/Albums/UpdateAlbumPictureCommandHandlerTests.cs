using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class UpdateAlbumPictureCommandHandlerTests : HandlerTestBase
    {
        private readonly IEventBus eventBus = Substitute.For<IEventBus>();
        private readonly IStorageService storageService = Substitute.For<IStorageService>();

        private UpdateAlbumPictureCommandHandler CreateHandler() => new(
            eventBus,
            Database,
            new UploadIntentValidator(Database, storageService, TestConfigurations.UploadIntent()),
            NoOpLogger<UpdateAlbumPictureCommandHandler>(),
            TestConfigurations.Album());

        [Fact]
        public async Task Handle_ValidPictureIntent_UpdatesPictureAndPublishesSourceEvent()
        {
            storageService
                .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ObjectMetaData("image/webp", 1024));

            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id, title: "Keep Title", description: "Keep description", releaseYear: 2020);
            var intent = TestEntities.UploadIntent(owner.Id, Musify.Domain.ValueObjects.UploadIntentPurpose.AlbumPicture, objectName: "new-cover.webp");
            await SeedAsync(owner, album, intent);

            var result = await CreateHandler().Handle(new UpdateAlbumPictureCommand(owner.Id, album.Id, intent.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("Keep Title", result.Value.Title);
            Assert.Equal("Keep description", result.Value.Description);
            Assert.Equal(2020, result.Value.ReleaseYear);
            var stored = await Database.Albums.FindAsync([album.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("new-cover.webp", stored.Pictures?.OriginalName);
            await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.UpdateAlbumPictureSourceEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new UpdateAlbumPictureCommand(1, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, stranger, album);

            var result = await CreateHandler().Handle(new UpdateAlbumPictureCommand(stranger.Id, album.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            await eventBus.DidNotReceive().PublishAsync(Arg.Any<Musify.Application.Events.UpdateAlbumPictureSourceEvent>(), Arg.Any<CancellationToken>());
        }
    }
}
