using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Users;

public sealed class IsFollowingUserQueryHandlerTests : HandlerTestBase
{
    private IsFollowingUserQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_Following_ReturnsTrue()
    {
        var follower = TestEntities.User(1, "follower");
        var followed = TestEntities.User(2, "followed");
        await SeedAsync(follower, followed, new UserFollow { FollowerId = follower.Id, FollowedId = followed.Id });

        var result = await CreateHandler().Handle(new IsFollowingUserQuery(follower.Id, followed.Id), TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_NotFollowing_ReturnsFalse()
    {
        var follower = TestEntities.User(1, "follower");
        var followed = TestEntities.User(2, "followed");
        await SeedAsync(follower, followed);

        var result = await CreateHandler().Handle(new IsFollowingUserQuery(follower.Id, followed.Id), TestContext.Current.CancellationToken);

        Assert.False(result);
    }
}
