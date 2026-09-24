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
        var olderAlbum = TestEntities.Album(owner.Id, "Older");
        var newerAlbum = TestEntities.Album(owner.Id, "Newer");
        var olderTrack = TestEntities.Track(owner, "In older album");
        var newerTrack = TestEntities.Track(owner, "In newer album");
        await SeedAsync(
            owner, olderAlbum, newerAlbum, olderTrack, newerTrack,
            new AlbumHasTrack { AlbumId = olderAlbum.Id, TrackId = olderTrack.Id, TrackNumber = 1 },
            new AlbumHasTrack { AlbumId = newerAlbum.Id, TrackId = newerTrack.Id, TrackNumber = 1 },
            TestEntities.ListeningHistory(owner.Id, olderTrack.Id, DateTime.UtcNow.AddDays(-2)),
            TestEntities.ListeningHistory(owner.Id, newerTrack.Id, DateTime.UtcNow.AddDays(-1)));

        var result = await CreateHandler().Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(["Newer", "Older"], result.Value.Select(album => album.Title));
    }

    [Fact]
    public async Task Handle_NoListeningHistory_ReturnsEmptyList()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner);

        var result = await CreateHandler().Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_LimitLowerThanCandidates_ReturnsOnlyThatManyAlbums()
    {
        var owner = TestEntities.User();
        var albums = Enumerable.Range(1, 3).Select(i => TestEntities.Album(owner.Id, $"Album {i}")).ToList();
        var tracks = albums.Select((_, i) => TestEntities.Track(owner, $"Track {i}")).ToList();
        var links = albums.Zip(tracks, (album, track) => new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 }).ToList();
        var histories = tracks.Select((track, i) => TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddMinutes(-i))).ToList();
        await SeedAsync([owner, .. albums, .. tracks, .. links, .. histories]);

        var result = await CreateHandler().Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 2), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_UncountedListens_AreIgnored()
    {
        var owner = TestEntities.User();
        var countedAlbum = TestEntities.Album(owner.Id, "Counted album");
        var skippedAlbum = TestEntities.Album(owner.Id, "Skipped album");
        var countedTrack = TestEntities.Track(owner, "In counted");
        var skippedTrack = TestEntities.Track(owner, "In skipped");
        await SeedAsync(
            owner, countedAlbum, skippedAlbum, countedTrack, skippedTrack,
            new AlbumHasTrack { AlbumId = countedAlbum.Id, TrackId = countedTrack.Id, TrackNumber = 1 },
            new AlbumHasTrack { AlbumId = skippedAlbum.Id, TrackId = skippedTrack.Id, TrackNumber = 1 },
            TestEntities.ListeningHistory(owner.Id, countedTrack.Id, DateTime.UtcNow.AddDays(-1), playedSeconds: 120),
            TestEntities.ListeningHistory(owner.Id, skippedTrack.Id, DateTime.UtcNow, playedSeconds: 1, isCounted: false));

        var result = await CreateHandler().Handle(new GetRecentlyListenedAlbumsQuery(owner.Id, Limit: 10), TestContext.Current.CancellationToken);

        Assert.Equal(["Counted album"], result.Value.Select(album => album.Title));
    }
}
