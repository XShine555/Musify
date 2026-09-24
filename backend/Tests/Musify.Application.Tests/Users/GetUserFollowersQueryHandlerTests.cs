using ErrorOr;
using Musify.Application.Shared;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Musify.Application.Users.Responses;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Users
{
    public sealed class GetUserFollowersQueryHandlerTests : HandlerTestBase
    {
        private GetUserFollowersQueryHandler CreateHandler() => new(Database);

        private async Task<ErrorOr<PaginatedResponse<UserSummaryResponse>>> Send(long? viewerId, long userId) =>
            await CreateHandler().Handle(new GetUserFollowersQuery(viewerId, userId, 1, 20), TestContext.Current.CancellationToken);

        [Fact]
        public async Task Handle_OwnProfile_ReturnsFollowers()
        {
            var user = TestEntities.User(1, "user");
            var follower = TestEntities.User(2, "follower");
            await SeedAsync(user, follower, new UserFollow { FollowerId = follower.Id, FollowedId = user.Id });

            var result = await Send(user.Id, user.Id);

            Assert.False(result.IsError);
            var item = Assert.Single(result.Value.Items);
            Assert.Equal(follower.Id, item.Id);
            Assert.False(item.IsFollowedByViewer);
        }

        [Fact]
        public async Task Handle_MutualFollow_ReturnsFollowersWithViewerFollowState()
        {
            var viewer = TestEntities.User(1, "viewer");
            var user = TestEntities.User(2, "user");
            var other = TestEntities.User(3, "other");
            await SeedAsync(
                viewer, user, other,
                new UserFollow { FollowerId = viewer.Id, FollowedId = user.Id },
                new UserFollow { FollowerId = user.Id, FollowedId = viewer.Id },
                new UserFollow { FollowerId = other.Id, FollowedId = user.Id },
                new UserFollow { FollowerId = viewer.Id, FollowedId = other.Id });

            var result = await Send(viewer.Id, user.Id);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.TotalItemCount);
            Assert.All(result.Value.Items, item => Assert.Equal(item.Id == other.Id, item.IsFollowedByViewer));
        }

        [Fact]
        public async Task Handle_ViewerFollowsButNotFollowedBack_ReturnsForbidden()
        {
            var viewer = TestEntities.User(1, "viewer");
            var user = TestEntities.User(2, "user");
            await SeedAsync(viewer, user, new UserFollow { FollowerId = viewer.Id, FollowedId = user.Id });

            var result = await Send(viewer.Id, user.Id);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_FollowedByUserButNotFollowingBack_ReturnsForbidden()
        {
            var viewer = TestEntities.User(1, "viewer");
            var user = TestEntities.User(2, "user");
            await SeedAsync(viewer, user, new UserFollow { FollowerId = user.Id, FollowedId = viewer.Id });

            var result = await Send(viewer.Id, user.Id);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_Anonymous_ReturnsForbidden()
        {
            await SeedAsync(TestEntities.User(1, "user"));

            var result = await Send(null, 1);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_UserMissing_ReturnsNotFound()
        {
            var result = await Send(1, 404);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
