using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Users
{
    public sealed class GetUserProfileQueryHandlerTests : HandlerTestBase
    {
        private GetUserProfileQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_ExistingUser_ReturnsFollowerAndFollowingCounts()
        {
            var user = TestEntities.User(1, "user");
            var followerA = TestEntities.User(2, "followerA");
            var followerB = TestEntities.User(3, "followerB");
            var followed = TestEntities.User(4, "followed");
            await SeedAsync(
                user, followerA, followerB, followed,
                new UserFollow { FollowerId = followerA.Id, FollowedId = user.Id },
                new UserFollow { FollowerId = followerB.Id, FollowedId = user.Id },
                new UserFollow { FollowerId = user.Id, FollowedId = followed.Id });

            var result = await CreateHandler().Handle(new GetUserProfileQuery(user.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.FollowersCount);
            Assert.Equal(1, result.Value.FollowingCount);
        }

        [Fact]
        public async Task Handle_UserMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetUserProfileQuery(404), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
