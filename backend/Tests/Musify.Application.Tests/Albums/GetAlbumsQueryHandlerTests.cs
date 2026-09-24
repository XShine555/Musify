using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Albums;

public sealed class GetAlbumsQueryHandlerTests : HandlerTestBase
{
    private GetAlbumsQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_NoTitleFilter_ReturnsAllAlbumsPaged()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner, TestEntities.Album(owner.Id, "Alpha"), TestEntities.Album(owner.Id, "Beta"));

        var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);
        Assert.Equal(2, result.TotalItemCount);
        Assert.All(result.Items, item => Assert.NotNull(item));
    }

    [Fact]
    public async Task Handle_TitleFilter_ReturnsMatchingAlbumsOnly()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner, TestEntities.Album(owner.Id, "Rock Classics"), TestEntities.Album(owner.Id, "Jazz Favorites"));

        var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: "rock", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Items);
        Assert.Equal("Rock Classics", item.Title);
    }

    [Fact]
    public async Task Handle_PageSizeSmallerThanTotal_ReportsHasNextPage()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner, TestEntities.Album(owner.Id, "One"), TestEntities.Album(owner.Id, "Two"), TestEntities.Album(owner.Id, "Three"));

        var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: null, PageNumber: 1, PageSize: 2), TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasNextPage);
    }
}
