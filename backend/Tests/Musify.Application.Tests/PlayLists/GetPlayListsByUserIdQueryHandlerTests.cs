using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class GetPlayListsByUserIdQueryHandlerTests : HandlerTestBase
{
    private GetPlayListsByUserIdQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_ReturnsOnlyActivePlayListsForThatUser()
    {
        var owner = TestEntities.User(1, "owner");
        var other = TestEntities.User(2, "other");
        await SeedAsync(
            owner, other,
            TestEntities.PlayList(owner.Id, "Mine"),
            TestEntities.PlayList(owner.Id, "Being removed", lifeCycleStatus: LifeCycleStatus.Removing),
            TestEntities.PlayList(other.Id, "Theirs"));

        var result = await CreateHandler().Handle(new GetPlayListsByUserIdQuery(owner.Id, Name: null, PageNumber: 1, PageSize: 10, ViewerId: owner.Id), TestContext.Current.CancellationToken);
        var playList = Assert.Single(result.Items);
        Assert.Equal("Mine", playList.Name);
    }

    [Fact]
    public async Task Handle_AnotherViewer_ReturnsOnlyPublicPlayLists()
    {
        var owner = TestEntities.User();
        await SeedAsync(
            owner,
            TestEntities.PlayList(owner.Id, "Public one", visibility: PlayListVisibility.Public),
            TestEntities.PlayList(owner.Id, "Private one", visibility: PlayListVisibility.Private));

        var result = await CreateHandler().Handle(new GetPlayListsByUserIdQuery(owner.Id, Name: null, PageNumber: 1, PageSize: 10, ViewerId: 999), TestContext.Current.CancellationToken);
        var playList = Assert.Single(result.Items);
        Assert.Equal("Public one", playList.Name);
    }

    [Fact]
    public async Task Handle_Owner_SeesPrivatePlayLists()
    {
        var owner = TestEntities.User();
        await SeedAsync(
            owner,
            TestEntities.PlayList(owner.Id, "Public one", visibility: PlayListVisibility.Public),
            TestEntities.PlayList(owner.Id, "Private one", visibility: PlayListVisibility.Private));

        var result = await CreateHandler().Handle(new GetPlayListsByUserIdQuery(owner.Id, Name: null, PageNumber: 1, PageSize: 10, ViewerId: owner.Id), TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_NameFilter_ReturnsOnlyMatchingPlayLists()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner, TestEntities.PlayList(owner.Id, "Road Trip"), TestEntities.PlayList(owner.Id, "Study Focus"));

        var result = await CreateHandler().Handle(new GetPlayListsByUserIdQuery(owner.Id, Name: "road", PageNumber: 1, PageSize: 10, ViewerId: owner.Id), TestContext.Current.CancellationToken);
        var playList = Assert.Single(result.Items);
        Assert.Equal("Road Trip", playList.Name);
    }
}
