using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class CreateAlbumCommandHandlerTests : HandlerTestBase
    {
        private readonly IEventBus eventBus = Substitute.For<IEventBus>();
        private readonly IStorageService storageService = Substitute.For<IStorageService>();

        private CreateAlbumCommandHandler CreateHandler() => new(
            eventBus,
            Database,
            new UploadIntentValidator(Database, storageService),
            NoOpLogger<CreateAlbumCommandHandler>(),
            TestConfigurations.Storage(),
            TestConfigurations.Album(),
            TestConfigurations.UploadIntent());

        [Fact]
        public async Task Handle_ValidPictureIntent_CreatesAlbumAndPublishesResourcesEvent()
        {
            storageService
                .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ObjectMetaData("image/webp", 1024));

            var owner = TestEntities.User();
            var intent = TestEntities.UploadIntent(owner.Id, Musify.Domain.ValueObjects.UploadIntentPurpose.AlbumPicture, objectName: "cover.webp");
            await SeedAsync(owner, intent);

            var command = new CreateAlbumCommand(owner.Id, "  My Album  ", "A description", 2024, intent.Id);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("My Album", result.Value.Title);
            Assert.Equal("A description", result.Value.Description);
            Assert.Equal(2024, result.Value.ReleaseYear);
            Assert.Equal(owner.Id, result.Value.OwnerUserId);
            Assert.Equal(0, result.Value.TrackCount);

            var stored = await Database.Albums.FindAsync([result.Value.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("MY ALBUM", stored.NormalizedTitle);
            Assert.Equal("cover.webp", stored.Pictures.OriginalName);
            await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.CreateAlbumResourcesEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_UserDoesNotExist_ReturnsNotFound()
        {
            var command = new CreateAlbumCommand(404, "Orphan Album", null, null, Guid.NewGuid());

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_PictureIntentNotFound_ReturnsError()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner);

            var command = new CreateAlbumCommand(owner.Id, "My Album", null, null, Guid.NewGuid());

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsError);
        }
    }
}
