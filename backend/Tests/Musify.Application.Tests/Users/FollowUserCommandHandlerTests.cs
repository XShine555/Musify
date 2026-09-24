using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Xunit;

namespace Musify.Application.Tests.Users;

public sealed class FollowUserCommandHandlerTests : HandlerTestBase
{
    private FollowUserCommandHandler CreateHandler() => new(Database, NoOpLogger<FollowUserCommandHandler>());

    [Fact]
    public async Task Handle_ValidTarget_CreatesFollow()
    {
        var follower = TestEntities.User(1, "follower");
        var followed = TestEntities.User(2, "followed");
        await SeedAsync(follower, followed);

        var result = await CreateHandler().Handle(new FollowUserCommand(follower.Id, followed.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Single(await Database.UserFollows.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_AlreadyFollowing_IsIdempotent()
    {
        var follower = TestEntities.User(1, "follower");
        var followed = TestEntities.User(2, "followed");
        await SeedAsync(follower, followed, new Musify.Domain.Entities.UserFollow { FollowerId = follower.Id, FollowedId = followed.Id });

        var result = await CreateHandler().Handle(new FollowUserCommand(follower.Id, followed.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Single(await Database.UserFollows.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_SelfFollow_ReturnsValidationError()
    {
        var user = TestEntities.User();
        await SeedAsync(user);

        var result = await CreateHandler().Handle(new FollowUserCommand(user.Id, user.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TargetMissing_ReturnsNotFound()
    {
        var follower = TestEntities.User();
        await SeedAsync(follower);

        var result = await CreateHandler().Handle(new FollowUserCommand(follower.Id, 404), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
