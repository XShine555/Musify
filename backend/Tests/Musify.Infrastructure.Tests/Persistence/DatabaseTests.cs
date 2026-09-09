using Microsoft.EntityFrameworkCore;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Musify.Infrastructure.Tests.Persistence;

[Collection(InfrastructureCollection.Name)]
public sealed class DatabaseTests(InfrastructureTestFixture fixture)
{
    [Fact]
    public async Task SaveChangesAsync_LocalTrackWithOwnedTypes_RoundTripsThroughPostgres()
    {
        await using var database = fixture.CreateDatabase();

        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = "roundtrip-user", NormalizedName = "ROUNDTRIP-USER" };
        await database.Users.AddAsync(user);

        var track = new LocalTrack
        {
            Title = "Round Trip Track",
            NormalizedTitle = "ROUND TRIP TRACK",
            DurationSeconds = 123.4,
            OwnerUserId = user.Id,
            Owner = user,
            Pictures = new TrackPictures { OriginalName = "cover.webp", ProcessingStatus = ProcessingStatus.Pending },
            Audio = new TrackAudio { OriginalName = "song.mp3", TranscodeStatus = ProcessingStatus.Pending }
        };
        await database.LocalTracks.AddAsync(track);
        await database.SaveChangesAsync(CancellationToken.None);

        await using var reloaded = fixture.CreateDatabase();
        var stored = await reloaded.Tracks.AsNoTracking().SingleAsync(t => t.Id == track.Id);

        Assert.IsType<LocalTrack>(stored);
        Assert.Equal("Round Trip Track", stored.Title);
        Assert.Equal("cover.webp", stored.Pictures.OriginalName);
        Assert.Equal("song.mp3", stored.Audio.OriginalName);
        Assert.Equal(user.Id, ((LocalTrack)stored).OwnerUserId);
    }

    [Fact]
    public async Task ExternalTracks_SourceAndExternalIdUniqueIndex_RejectsDuplicates()
    {
        await using var database = fixture.CreateDatabase();

        var videoId = $"dup-{Guid.NewGuid():N}";
        await database.ExternalTracks.AddAsync(BuildExternalTrack(videoId));
        await database.SaveChangesAsync(CancellationToken.None);

        await using var second = fixture.CreateDatabase();
        await second.ExternalTracks.AddAsync(BuildExternalTrack(videoId));

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AlbumHasTracks_DeletingAlbum_CascadesToTheLinkRows()
    {
        await using var database = fixture.CreateDatabase();

        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = "cascade-user", NormalizedName = "CASCADE-USER" };
        var album = new UserAlbum { Title = "Cascade Album", NormalizedTitle = "CASCADE ALBUM", OwnerUserId = user.Id };
        var track = BuildExternalTrack($"cascade-{Guid.NewGuid():N}");
        var link = new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 };

        await database.AddRangeAsync(user, album, track, link);
        await database.SaveChangesAsync(CancellationToken.None);

        await using var deleter = fixture.CreateDatabase();
        var albumToDelete = await deleter.UserAlbums.SingleAsync(a => a.Id == album.Id);
        deleter.UserAlbums.Remove(albumToDelete);
        await deleter.SaveChangesAsync(CancellationToken.None);

        await using var verifier = fixture.CreateDatabase();
        Assert.False(await verifier.AlbumHasTracks.AnyAsync(l => l.AlbumId == album.Id));
        Assert.True(await verifier.ExternalTracks.AnyAsync(t => t.Id == track.Id));
    }

    private static ExternalTrack BuildExternalTrack(string externalId) => new()
    {
        Title = "External",
        NormalizedTitle = "EXTERNAL",
        DurationSeconds = 100,
        Source = TrackSource.YouTube,
        ExternalId = externalId,
        Pictures = new TrackPictures(),
        Audio = new TrackAudio()
    };
}
