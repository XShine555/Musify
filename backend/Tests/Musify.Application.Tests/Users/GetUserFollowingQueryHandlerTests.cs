using ErrorOr;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Users
{
    public sealed class GetUserFollowingQueryHandlerTests : HandlerTestBase
    {
        private GetUserFollowingQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_Anonymous_ReturnsFollowedUsers()
        {
            var user = TestEntities.User(1, "user");
            var followedA = TestEntities.User(2, "followedA");
            var followedB = TestEntities.User(3, "followedB");
            await SeedAsync(
                user, followedA, followedB,
                new UserFollow { FollowerId = user.Id, FollowedId = followedA.Id },
                new UserFollow { FollowerId = user.Id, FollowedId = followedB.Id });

            var result = await CreateHandler().Handle(new GetUserFollowingQuery(null, user.Id, 1, 20), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.TotalItemCount);
            Assert.All(result.Value.Items, item => Assert.False(item.IsFollowedByViewer));
        }

        [Fact]
        public async Task Handle_Viewer_MarksUsersTheViewerFollows()
        {
            var viewer = TestEntities.User(1, "viewer");
            var user = TestEntities.User(2, "user");
            var followedA = TestEntities.User(3, "followedA");
            var followedB = TestEntities.User(4, "followedB");
            await SeedAsync(
                viewer, user, followedA, followedB,
                new UserFollow { FollowerId = user.Id, FollowedId = followedA.Id },
                new UserFollow { FollowerId = user.Id, FollowedId = followedB.Id },
                new UserFollow { FollowerId = viewer.Id, FollowedId = followedA.Id });

            var result = await CreateHandler().Handle(new GetUserFollowingQuery(viewer.Id, user.Id, 1, 20), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.All(result.Value.Items, item => Assert.Equal(item.Id == followedA.Id, item.IsFollowedByViewer));
        }

        [Fact]
        public async Task Handle_Pagination_ReturnsRequestedPage()
        {
            await SeedAsync(
                TestEntities.User(1, "user"), TestEntities.User(2, "a"), TestEntities.User(3, "b"), TestEntities.User(4, "c"),
                new UserFollow { FollowerId = 1, FollowedId = 2 },
                new UserFollow { FollowerId = 1, FollowedId = 3 },
                new UserFollow { FollowerId = 1, FollowedId = 4 });

            var result = await CreateHandler().Handle(new GetUserFollowingQuery(null, 1, 2, 2), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Single(result.Value.Items);
            Assert.Equal(3, result.Value.TotalItemCount);
            Assert.False(result.Value.HasNextPage);
        }

        [Fact]
        public async Task Handle_UserMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetUserFollowingQuery(null, 404, 1, 20), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
