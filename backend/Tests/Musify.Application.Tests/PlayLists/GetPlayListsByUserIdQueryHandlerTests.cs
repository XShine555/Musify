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

        var result = await CreateHandler().Handle(new GetPlayListsByUserIdQuery(owner.Id, Name: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var playList = Assert.Single(result.Value.Items);
        Assert.Equal("Mine", playList.Name);
    }

    [Fact]
    public async Task Handle_OnlyPublic_ReturnsOnlyPublicPlayLists()
    {
        var owner = TestEntities.User();
        await SeedAsync(
            owner,
            TestEntities.PlayList(owner.Id, "Public one", visibility: PlaylistVisibility.Public),
            TestEntities.PlayList(owner.Id, "Private one", visibility: PlaylistVisibility.Private));

        var result = await CreateHandler().Handle(new GetPlayListsByUserIdQuery(owner.Id, Name: null, PageNumber: 1, PageSize: 10, OnlyPublic: true), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var playList = Assert.Single(result.Value.Items);
        Assert.Equal("Public one", playList.Name);
    }

    [Fact]
    public async Task Handle_NameFilter_ReturnsOnlyMatchingPlayLists()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner, TestEntities.PlayList(owner.Id, "Road Trip"), TestEntities.PlayList(owner.Id, "Study Focus"));

        var result = await CreateHandler().Handle(new GetPlayListsByUserIdQuery(owner.Id, Name: "road", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var playList = Assert.Single(result.Value.Items);
        Assert.Equal("Road Trip", playList.Name);
    }
}
