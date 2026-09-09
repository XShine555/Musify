using ErrorOr;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class GetPlayListCoverQueryHandlerTests : HandlerTestBase
{
    private GetPlayListCoverQueryHandler CreateHandler() =>
        new(Database, TestConfigurations.Storage("covers-bucket"), TestConfigurations.PlayList());

    [Theory]
    [InlineData("small", "small.webp")]
    [InlineData("large", "large.webp")]
    [InlineData("medium", "medium.webp")]
    public async Task Handle_ExistingPlayList_ReturnsRequestedSize(string size, string expectedFileName)
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var result = await CreateHandler().Handle(new GetPlayListCoverQuery(playList.Id, size), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("covers-bucket", result.Value.Bucket);
        Assert.EndsWith(expectedFileName, result.Value.Key);
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new GetPlayListCoverQuery(Guid.NewGuid(), "small"), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
