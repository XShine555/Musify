using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Xunit;

namespace Musify.Application.Tests.Users
{
    public sealed class CreateUserCommandHandlerTests : HandlerTestBase
    {
        private CreateUserCommandHandler CreateHandler() => new(Database, NoOpLogger<CreateUserCommandHandler>());

        [Fact]
        public async Task Handle_NewId_CreatesUser()
        {
            var command = new CreateUserCommand(1, "Jane Doe", "Jane", "Doe");

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("Jane Doe", result.Value.Name);
            Assert.Equal(1, result.Value.Id);

            var stored = await Database.Users.FindAsync([1L], TestContext.Current.CancellationToken);
            Assert.NotNull(stored);
            Assert.Equal("JANE DOE", stored.NormalizedName);
        }

        [Fact]
        public async Task Handle_IdAlreadyExists_ReturnsConflict()
        {
            var existing = TestEntities.User(1);
            await SeedAsync(existing);

            var command = new CreateUserCommand(1, "Someone Else", null, null);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        }
    }
}
