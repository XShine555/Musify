using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class GetPlayListsQueryHandlerTests : HandlerTestBase
{
    private GetPlayListsQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_ReturnsEveryPlayListPaged()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner, TestEntities.PlayList(owner.Id, "One"), TestEntities.PlayList(owner.Id, "Two"));

        var result = await CreateHandler().Handle(new GetPlayListsQuery(PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.TotalItemCount);
    }

    [Fact]
    public async Task Handle_NoPlayLists_ReturnsEmptyPage()
    {
        var result = await CreateHandler().Handle(new GetPlayListsQuery(PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(result.Value.Items);
    }
}
