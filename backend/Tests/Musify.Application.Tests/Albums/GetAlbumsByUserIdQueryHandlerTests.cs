using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Albums;

public sealed class GetAlbumsByUserIdQueryHandlerTests : HandlerTestBase
{
    private GetAlbumsByUserIdQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_UserWithAlbums_ReturnsOnlyTheirAlbums()
    {
        var owner = TestEntities.User(1, "owner");
        var other = TestEntities.User(2, "other");
        await SeedAsync(owner, other, TestEntities.UserAlbum(owner.Id, "Mine"), TestEntities.UserAlbum(other.Id, "Theirs"));

        var result = await CreateHandler().Handle(new GetAlbumsByUserIdQuery(owner.Id, PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.False(result.IsError);
        var album = Assert.Single(result.Value.Items);
        Assert.Equal("Mine", album.Title);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new GetAlbumsByUserIdQuery(404, PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
