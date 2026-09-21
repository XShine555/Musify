using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Xunit;

namespace Musify.Application.Tests.Users
{
    public sealed class SyncUserCommandHandlerTests : HandlerTestBase
    {
        private SyncUserCommandHandler CreateHandler() => new(Database, NoOpLogger<SyncUserCommandHandler>());

        [Fact]
        public async Task Handle_UnknownUser_ProvisionsIt()
        {
            var command = new SyncUserCommand(1, "Jane Doe", "Jane", "Doe", "https://avatar.example/jane.png");

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var stored = await Database.Users.FindAsync([1L], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("Jane Doe", stored.Name);
            Assert.Equal("https://avatar.example/jane.png", stored.ProfilePictureUrl);
        }

        [Fact]
        public async Task Handle_ExistingUser_UpdatesProfileFields()
        {
            var user = TestEntities.User(1, "Old Name", profilePictureUrl: null);
            await SeedAsync(user);

            var command = new SyncUserCommand(1, "New Name", "New", "Name", "https://avatar.example/new.png");

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var stored = await Database.Users.FindAsync([1L], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("New Name", stored.Name);
            Assert.Equal("NEW NAME", stored.NormalizedName);
            Assert.Equal("https://avatar.example/new.png", stored.ProfilePictureUrl);
        }
    }
}
