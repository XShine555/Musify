using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Xunit;

namespace Musify.Application.Tests.Users
{
    public sealed class GetUserByIdQueryHandlerTests : HandlerTestBase
    {
        private GetUserByIdQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_ExistingUser_ReturnsIt()
        {
            var user = TestEntities.User(1, "Jane Doe");
            await SeedAsync(user);

            var result = await CreateHandler().Handle(new GetUserByIdQuery(1), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("Jane Doe", result.Value.Name);
        }

        [Fact]
        public async Task Handle_UserMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetUserByIdQuery(404), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
