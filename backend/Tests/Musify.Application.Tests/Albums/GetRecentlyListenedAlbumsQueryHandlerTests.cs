using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Albums;

public sealed class GetRecentlyListenedAlbumsQueryHandlerTests : HandlerTestBase
{
    private GetRecentlyListenedAlbumsQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_ListenedTracks_ReturnsTheirAlbumsMostRecentFirst()
    {
        var owner = TestEntities.User();
        var olderAlbum = TestEntities.UserAlbum(owner.Id, "Older");
        var newerAlbum = TestEntities.UserAlbum(owner.Id, "Newer");
        var olderTrack = TestEntities.LocalTrack(owner, "In older album");
        var newerTrack = TestEntities.LocalTrack(owner, "In newer album");
        await SeedAsync(
            owner, olderAlbum, newerAlbum, olderTrack, newerTrack,
            new AlbumHasTrack { AlbumId = olderAlbum.Id, TrackId = olderTrack.Id, TrackNumber = 1 },
            new AlbumHasTrack { AlbumId = newerAlbum.Id, TrackId = newerTrack.Id, TrackNumber = 1 },
            TestEntities.ListeningHistory(owner.Id, olderTrack.Id, DateTime.UtcNow.AddDays(-2)),
            TestEntities.ListeningHistory(owner.Id, newerTrack.Id, DateTime.UtcNow.AddDays(-1)));

        var result = await CreateHandler().Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 10), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(["Newer", "Older"], result.Value.Select(album => album.Title));
    }

    [Fact]
    public async Task Handle_NoListeningHistory_ReturnsEmptyList()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner);

        var result = await CreateHandler().Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 10), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_LimitLowerThanCandidates_ReturnsOnlyThatManyAlbums()
    {
        var owner = TestEntities.User();
        var albums = Enumerable.Range(1, 3).Select(i => TestEntities.UserAlbum(owner.Id, $"Album {i}")).ToList();
        var tracks = albums.Select((_, i) => TestEntities.LocalTrack(owner, $"Track {i}")).ToList();
        var links = albums.Zip(tracks, (album, track) => new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 }).ToList();
        var histories = tracks.Select((track, i) => TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddMinutes(-i))).ToList();
        await SeedAsync([owner, .. albums, .. tracks, .. links, .. histories]);

        var result = await CreateHandler().Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 2), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count);
    }
}
