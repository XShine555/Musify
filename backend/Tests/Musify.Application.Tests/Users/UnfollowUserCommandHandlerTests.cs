using Microsoft.EntityFrameworkCore;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Users;

public sealed class UnfollowUserCommandHandlerTests : HandlerTestBase
{
    private UnfollowUserCommandHandler CreateHandler() => new(Database, NoOpLogger<UnfollowUserCommandHandler>());

    [Fact]
    public async Task Handle_ExistingFollow_RemovesIt()
    {
        var follower = TestEntities.User(1, "follower");
        var followed = TestEntities.User(2, "followed");
        await SeedAsync(follower, followed, new UserFollow { FollowerId = follower.Id, FollowedId = followed.Id });

        var result = await CreateHandler().Handle(new UnfollowUserCommand(follower.Id, followed.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Empty(await Database.UserFollows.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NotFollowing_IsIdempotent()
    {
        var follower = TestEntities.User(1, "follower");
        var followed = TestEntities.User(2, "followed");
        await SeedAsync(follower, followed);

        var result = await CreateHandler().Handle(new UnfollowUserCommand(follower.Id, followed.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
    }
}
