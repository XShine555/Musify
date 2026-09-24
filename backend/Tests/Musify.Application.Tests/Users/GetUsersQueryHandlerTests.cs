using Musify.Application.Tests.TestSupport;
using Musify.Application.Users;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Users;

public sealed class GetUsersQueryHandlerTests : HandlerTestBase
{
    private GetUsersQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_NoSearch_ReturnsEveryUser()
    {
        await SeedAsync(TestEntities.User(1, "alice"), TestEntities.User(2, "bob"));

        var result = await CreateHandler().Handle(new GetUsersQuery(PageNumber: 1, PageSize: 10, UsernameSearch: null), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.TotalItemCount);
    }

    [Fact]
    public async Task Handle_UsernameSearch_ReturnsOnlyMatches()
    {
        await SeedAsync(TestEntities.User(1, "alice-wonderland"), TestEntities.User(2, "bob-builder"));

        var result = await CreateHandler().Handle(new GetUsersQuery(PageNumber: 1, PageSize: 10, UsernameSearch: "alice"), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var user = Assert.Single(result.Value.Items);
        Assert.Equal(1, user.Id);
    }

    [Fact]
    public async Task Handle_Viewer_MarksFollowedUsers()
    {
        await SeedAsync(
            TestEntities.User(1, "viewer"), TestEntities.User(2, "alice"), TestEntities.User(3, "bob"),
            new UserFollow { FollowerId = 1, FollowedId = 2 });

        var result = await CreateHandler().Handle(new GetUsersQuery(PageNumber: 1, PageSize: 10, UsernameSearch: null, ViewerId: 1), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.All(result.Value.Items, item => Assert.Equal(item.Id == 2, item.IsFollowedByViewer));
    }
}
