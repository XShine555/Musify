using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Albums;

public sealed class GetAlbumTracksQueryHandlerTests : HandlerTestBase
{
    private GetAlbumTracksQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_AlbumWithTracks_ReturnsThemOrderedByTrackNumber()
    {
        var owner = TestEntities.User();
        var album = TestEntities.UserAlbum(owner.Id);
        var second = TestEntities.LocalTrack(owner, "Second");
        var first = TestEntities.LocalTrack(owner, "First");
        await SeedAsync(
            owner, album, second, first,
            new AlbumHasTrack { AlbumId = album.Id, TrackId = second.Id, TrackNumber = 2 },
            new AlbumHasTrack { AlbumId = album.Id, TrackId = first.Id, TrackNumber = 1 });

        var result = await CreateHandler().Handle(new GetAlbumTracksQuery(album.Id, PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(["First", "Second"], result.Value.Items.Select(track => track.Title));
    }

    [Fact]
    public async Task Handle_AlbumMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new GetAlbumTracksQuery(Guid.NewGuid(), PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
