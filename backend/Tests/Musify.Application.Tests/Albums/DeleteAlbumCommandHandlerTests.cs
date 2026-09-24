using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Contracts;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class DeleteAlbumCommandHandlerTests : HandlerTestBase
    {
        private readonly IEventBus eventBus = Substitute.For<IEventBus>();

        private DeleteAlbumCommandHandler CreateHandler() => new(Database, eventBus, NoOpLogger<DeleteAlbumCommandHandler>());

        [Fact]
        public async Task Handle_Owner_MarksAlbumAsRemoving()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, album);

            var result = await CreateHandler().Handle(new DeleteAlbumCommand(owner.Id, album.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var stored = await Database.Albums.FindAsync([album.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal(LifeCycleStatus.Removing, stored.LifeCycleStatus);
            await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.DeleteAlbumEvent>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new DeleteAlbumCommand(1, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsForbiddenAndKeepsAlbum()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, stranger, album);

            var result = await CreateHandler().Handle(new DeleteAlbumCommand(stranger.Id, album.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
            Assert.NotNull(await Database.Albums.FindAsync([album.Id], TestContext.Current.CancellationToken));
        }
    }
}
