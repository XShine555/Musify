using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class CreateTrackCommandHandlerTests : HandlerTestBase
    {
        private readonly IEventBus eventBus = Substitute.For<IEventBus>();
        private readonly IStorageService storageService = Substitute.For<IStorageService>();

        private CreateTrackCommandHandler CreateHandler()
        {
            storageService
                .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ObjectMetaData("image/webp", 1024));

            return new CreateTrackCommandHandler(
                Database,
                eventBus,
                new UploadIntentValidator(Database, storageService, TestConfigurations.UploadIntent()),
                NoOpLogger<CreateTrackCommandHandler>(),
                TestConfigurations.Track());
        }

        private static (Domain.Entities.UploadIntent Picture, Domain.Entities.UploadIntent Audio) SeedIntents(long userId) =>
            (TestEntities.UploadIntent(userId, UploadIntentPurpose.TrackPicture, objectName: "cover.webp"),
             TestEntities.UploadIntent(userId, UploadIntentPurpose.TrackAudio, objectName: "song.mp3"));

        [Fact]
        public async Task Handle_SameIntentForPictureAndAudio_ReturnsValidationError()
        {
            var user = TestEntities.User();
            var (picture, audio) = SeedIntents(user.Id);
            await SeedAsync(user, picture, audio);

            var command = new CreateTrackCommand(user.Id, "My Song", picture.Id, picture.Id, [Genre.Pop]);
            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_ValidIntents_CreatesTrackAndPublishesEvent()
        {
            var user = TestEntities.User();
            var (picture, audio) = SeedIntents(user.Id);
            await SeedAsync(user, picture, audio);

            var command = new CreateTrackCommand(user.Id, "My Song", picture.Id, audio.Id, [Genre.Pop, Genre.Rock]);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("My Song", result.Value.Title);
            var localResponse = Assert.IsType<TrackApplicationResponse>(result.Value);
            Assert.Equal(user.Id, localResponse.OwnerUserId);
            Assert.Equal([Genre.Pop, Genre.Rock], localResponse.Tags);
            await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.CreateTrackResourcesEvent>(), Arg.Any<CancellationToken>());

            var stored = await Database.Tracks.FindAsync([result.Value.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal(ProcessingStatus.Pending, stored.Audio.TranscodeStatus);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_StoresExplicitFlag(bool isExplicit)
        {
            var user = TestEntities.User();
            var (picture, audio) = SeedIntents(user.Id);
            await SeedAsync(user, picture, audio);

            var command = new CreateTrackCommand(user.Id, "My Song", picture.Id, audio.Id, [Genre.Pop], isExplicit);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(isExplicit, result.Value.IsExplicit);
            var stored = await Database.Tracks.FindAsync([result.Value.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal(isExplicit, stored.IsExplicit);
        }

        [Fact]
        public async Task Handle_UserMissing_ReturnsNotFound()
        {
            var command = new CreateTrackCommand(404, "Orphan Song", Guid.NewGuid(), Guid.NewGuid(), [Genre.Pop]);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_PictureIntentBelongsToAnotherUser_ReturnsError()
        {
            var user = TestEntities.User(1, "user");
            var otherUser = TestEntities.User(2, "other");
            var (picture, audio) = SeedIntents(otherUser.Id);
            await SeedAsync(user, otherUser, picture, audio);

            var command = new CreateTrackCommand(user.Id, "My Song", picture.Id, audio.Id, [Genre.Pop]);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.True(result.IsError);
            await eventBus.DidNotReceive().PublishAsync(Arg.Any<Musify.Application.Events.CreateTrackResourcesEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_AudioIntentAlreadyConsumed_ReturnsError()
        {
            var user = TestEntities.User();
            var picture = TestEntities.UploadIntent(user.Id, UploadIntentPurpose.TrackPicture, objectName: "cover.webp");
            var audio = TestEntities.UploadIntent(user.Id, UploadIntentPurpose.TrackAudio, objectName: "song.mp3", status: UploadIntentStatus.Consumed);
            await SeedAsync(user, picture, audio);

            var command = new CreateTrackCommand(user.Id, "My Song", picture.Id, audio.Id, [Genre.Pop]);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        }
    }
}
