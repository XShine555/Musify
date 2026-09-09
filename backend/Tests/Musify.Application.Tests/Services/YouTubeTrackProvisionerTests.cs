using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Services;

public sealed class YouTubeTrackProvisionerTests : HandlerTestBase
{
    private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

    private YouTubeTrackProvisioner CreateProvisioner() =>
        new(Database, youTubeMusicService, TestConfigurations.Track(), NoOpLogger<YouTubeTrackProvisioner>());

    [Fact]
    public async Task FindAsync_UnknownVideo_ReturnsNull()
    {
        var track = await CreateProvisioner().FindAsync("missing-video", CancellationToken.None);

        Assert.Null(track);
    }

    [Fact]
    public async Task GetOrCreateAsync_KnownVideo_ReturnsExistingTrackWithoutCallingTheService()
    {
        var existing = TestEntities.ExternalTrack(externalId: "known-video");
        await SeedAsync(existing);

        var result = await CreateProvisioner().GetOrCreateAsync("known-video", CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(existing.Id, result.Value.Track.Id);
        await youTubeMusicService.DidNotReceiveWithAnyArgs().GetSongAsync(default!, default);
    }

    [Fact]
    public async Task GetOrCreateAsync_NewVideo_CreatesTrackAndDeduplicatesArtistsByName()
    {
        youTubeMusicService.GetSongAsync("new-video", Arg.Any<CancellationToken>()).Returns(
            new YouTubeSongResult(
                "new-video", "New Song", "New Artist", "Album", 210, "https://img/thumb.jpg", false,
                [new YouTubeArtistRef(null, "New Artist"), new YouTubeArtistRef(null, "new artist")]));

        var result = await CreateProvisioner().GetOrCreateAsync("new-video", CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("New Song", result.Value.Track.Title);
        Assert.Single(await Database.Artists.ToListAsync());
        Assert.Single(await Database.TrackArtists.ToListAsync());
    }

    [Fact]
    public async Task GetOrCreateAsync_ServiceFails_ReturnsError()
    {
        youTubeMusicService.GetSongAsync("bad-video", Arg.Any<CancellationToken>())
            .Returns(ErrorOr.Error.NotFound(description: "Video not found"));

        var result = await CreateProvisioner().GetOrCreateAsync("bad-video", CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task MaterializeAlbumAsync_NewAlbum_CreatesAlbumTracksAndLinks()
    {
        var detail = new YouTubeAlbumDetail(
            new YouTubeAlbumResult("album-1", "Album Title", "Album Artist", "https://img/thumb.jpg", 2024, false, false,
                [new YouTubeArtistRef(null, "Album Artist")]),
            Description: "desc",
            TotalDurationSeconds: 400,
            Tracks:
            [
                new YouTubeAlbumTrack("track-1", "Track One", 200, 1, false),
                new YouTubeAlbumTrack("track-2", "Track Two", 200, 2, false)
            ]);

        await CreateProvisioner().MaterializeAlbumAsync("album-1", detail, CancellationToken.None);

        Assert.Equal(2, await Database.ExternalTracks.CountAsync());
        Assert.Equal(2, await Database.AlbumHasTracks.CountAsync());
        Assert.Single(await Database.ExternalAlbums.ToListAsync());
    }

    [Fact]
    public async Task MaterializeAlbumAsync_CalledTwice_DoesNotDuplicateTracksOrLinks()
    {
        var detail = new YouTubeAlbumDetail(
            new YouTubeAlbumResult("album-1", "Album Title", "Album Artist", "https://img/thumb.jpg", 2024, false, false, []),
            Description: "desc",
            TotalDurationSeconds: 200,
            Tracks: [new YouTubeAlbumTrack("track-1", "Track One", 200, 1, false)]);

        var provisioner = CreateProvisioner();
        await provisioner.MaterializeAlbumAsync("album-1", detail, CancellationToken.None);
        await provisioner.MaterializeAlbumAsync("album-1", detail, CancellationToken.None);

        Assert.Single(await Database.ExternalTracks.ToListAsync());
        Assert.Single(await Database.AlbumHasTracks.ToListAsync());
        Assert.Single(await Database.ExternalAlbums.ToListAsync());
    }
}
