using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class UpdateAlbumCommandHandlerTests : HandlerTestBase
    {
        private UpdateAlbumCommandHandler CreateHandler() => new(Database, NoOpLogger<UpdateAlbumCommandHandler>());

        [Fact]
        public async Task Handle_Owner_UpdatesTitleDescriptionAndReleaseYear()
        {
            var owner = TestEntities.User();
            var album = TestEntities.UserAlbum(owner.Id, title: "Old Title", description: "Old", releaseYear: 2020);
            await SeedAsync(owner, album);

            var command = new UpdateAlbumCommand(owner.Id, album.Id, "  New Title  ", "New description", 2025);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("New Title", result.Value.Title);
            Assert.Equal("New description", result.Value.Description);
            Assert.Equal(2025, result.Value.ReleaseYear);

            var stored = await Database.UserAlbums.FindAsync([album.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("NEW TITLE", stored.NormalizedTitle);
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var command = new UpdateAlbumCommand(1, Guid.NewGuid(), "Title", null, null);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsUnauthorized()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var album = TestEntities.UserAlbum(owner.Id);
            await SeedAsync(owner, stranger, album);

            var command = new UpdateAlbumCommand(stranger.Id, album.Id, "Hijacked", null, null);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        }
    }
}
