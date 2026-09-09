using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class CreateAlbumCommandHandlerTests : HandlerTestBase
    {
        private CreateAlbumCommandHandler CreateHandler() => new(Database, NoOpLogger<CreateAlbumCommandHandler>());

        [Fact]
        public async Task Handle_UserExists_CreatesAlbumAndReturnsIt()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner);

            var command = new CreateAlbumCommand(owner.Id, "  My Album  ", "A description", 2024);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("My Album", result.Value.Title);
            Assert.Equal("A description", result.Value.Description);
            Assert.Equal(2024, result.Value.ReleaseYear);
            Assert.Equal(owner.Id, result.Value.OwnerUserId);
            Assert.Equal(0, result.Value.TrackCount);

            var stored = await Database.UserAlbums.FindAsync([result.Value.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("MY ALBUM", stored.NormalizedTitle);
        }

        [Fact]
        public async Task Handle_UserDoesNotExist_ReturnsNotFound()
        {
            var command = new CreateAlbumCommand(UserId: 404, "Orphan Album", Description: null, ReleaseYear: null);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
