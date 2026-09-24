using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Albums;

public sealed class DeleteAlbumCommandHandlerTests : HandlerTestBase
{
    private DeleteAlbumCommandHandler CreateHandler() => new(Database, NoOpLogger<DeleteAlbumCommandHandler>());

    [Fact]
    public async Task Handle_Owner_RemovesAlbum()
    {
        var owner = TestEntities.User();
        var album = TestEntities.Album(owner.Id);
        await SeedAsync(owner, album);

        var result = await CreateHandler().Handle(new DeleteAlbumCommand(owner.Id, album.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Null(await Database.Albums.FindAsync([album.Id], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_AlbumMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new DeleteAlbumCommand(1, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorizedAndKeepsAlbum()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var album = TestEntities.Album(owner.Id);
        await SeedAsync(owner, stranger, album);

        var result = await CreateHandler().Handle(new DeleteAlbumCommand(stranger.Id, album.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        Assert.NotNull(await Database.Albums.FindAsync([album.Id], TestContext.Current.CancellationToken));
    }
}
