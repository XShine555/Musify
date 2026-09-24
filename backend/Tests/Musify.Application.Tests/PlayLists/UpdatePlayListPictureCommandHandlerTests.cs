using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.PlayLists;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.PlayLists
{
    public sealed class UpdatePlayListPictureCommandHandlerTests : HandlerTestBase
    {
        private readonly IEventBus eventBus = Substitute.For<IEventBus>();
        private readonly IStorageService storageService = Substitute.For<IStorageService>();

        private UpdatePlayListPictureCommandHandler CreateHandler() => new(
            eventBus,
            Database,
            new UploadIntentValidator(Database, storageService, TestConfigurations.UploadIntent()),
            NoOpLogger<UpdatePlayListPictureCommandHandler>(),
            TestConfigurations.PlayList());

        [Fact]
        public async Task Handle_ValidPictureIntent_SetsPictureAndPublishesSourceEvent()
        {
            storageService
                .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ObjectMetaData("image/webp", 1024));

            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, "Keep Name", "Keep description");
            playList.Pictures = null;
            var intent = TestEntities.UploadIntent(owner.Id, Musify.Domain.ValueObjects.UploadIntentPurpose.PlayListPicture, objectName: "new-cover.webp");
            await SeedAsync(owner, playList, intent);

            var result = await CreateHandler().Handle(new UpdatePlayListPictureCommand(owner.Id, playList.Id, intent.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("Keep Name", result.Value.Name);
            Assert.Equal("Keep description", result.Value.Description);
            var stored = await Database.PlayLists.FindAsync([playList.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("new-cover.webp", stored.Pictures?.OriginalName);
            await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.UpdatePlayListPictureSourceEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_PlayListMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new UpdatePlayListPictureCommand(1, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var playList = TestEntities.PlayList(owner.Id);
            await SeedAsync(owner, stranger, playList);

            var result = await CreateHandler().Handle(new UpdatePlayListPictureCommand(stranger.Id, playList.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            await eventBus.DidNotReceive().PublishAsync(Arg.Any<Musify.Application.Events.UpdatePlayListPictureSourceEvent>(), Arg.Any<CancellationToken>());
        }
    }
}
