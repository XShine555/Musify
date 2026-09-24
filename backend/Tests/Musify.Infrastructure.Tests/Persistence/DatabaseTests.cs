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
    public async Task SaveChangesAsync_TrackWithOwnedTypes_RoundTripsThroughPostgres()
    {
        await using var database = fixture.CreateDatabase();

        var userName = "roundtrip-user";
        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = userName, NormalizedName = userName.ToUpperInvariant() };
        await database.Users.AddAsync(user, TestContext.Current.CancellationToken);

        var trackTitle = "Round Trip Track";
        var track = new Track
        {
            Title = trackTitle,
            NormalizedTitle = trackTitle.ToUpperInvariant(),
            DurationSeconds = 123.4,
            OwnerUserId = user.Id,
            Owner = user,
            Pictures = new TrackPictures { OriginalName = "cover.webp", ProcessingStatus = ProcessingStatus.Pending },
            Audio = new TrackAudio { OriginalName = "song.mp3", TranscodeStatus = ProcessingStatus.Pending }
        };
        await database.Tracks.AddAsync(track, TestContext.Current.CancellationToken);
        await database.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var reloaded = fixture.CreateDatabase();
        var stored = await reloaded.Tracks.AsNoTracking().SingleAsync(t => t.Id == track.Id, TestContext.Current.CancellationToken);

        Assert.Equal(trackTitle, stored.Title);
        Assert.Equal("cover.webp", stored.Pictures.OriginalName);
        Assert.Equal("song.mp3", stored.Audio.OriginalName);
        Assert.Equal(user.Id, stored.OwnerUserId);
    }

    [Fact]
    public async Task SaveChangesAsync_TrackWithTags_RoundTripsThroughNativePostgresEnum()
    {
        await using var database = fixture.CreateDatabase();

        var userName = "tags-roundtrip-user";
        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = userName, NormalizedName = userName.ToUpperInvariant() };
        var track = BuildTrack(user, $"tags-{Guid.NewGuid():N}");
        track.Tags =
        [
            new TrackTag { TrackId = track.Id, Tag = Genre.Pop },
            new TrackTag { TrackId = track.Id, Tag = Genre.Rock }
        ];

        await database.AddRangeAsync(user, track);
        await database.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var reloaded = fixture.CreateDatabase();
        var stored = await reloaded.Tracks.AsNoTracking()
            .Include(t => t.Tags)
            .SingleAsync(t => t.Id == track.Id, TestContext.Current.CancellationToken);

        Assert.Equal([Genre.Pop, Genre.Rock], stored.Tags.Select(t => t.Tag).OrderBy(t => t));
    }

    [Fact]
    public async Task AlbumHasTracks_DeletingAlbum_CascadesToTheLinkRows()
    {
        await using var database = fixture.CreateDatabase();

        var userName = "cascade-user";
        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = userName, NormalizedName = userName.ToUpperInvariant() };
        var albumTitle = "Cascade Album";
        var album = new Album { Title = albumTitle, NormalizedTitle = albumTitle.ToUpperInvariant(), OwnerUserId = user.Id };
        var track = BuildTrack(user, $"cascade-{Guid.NewGuid():N}");
        var link = new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 };

        await database.AddRangeAsync(user, album, track, link);
        await database.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var deleter = fixture.CreateDatabase();
        var albumToDelete = await deleter.Albums.SingleAsync(a => a.Id == album.Id, TestContext.Current.CancellationToken);
        deleter.Albums.Remove(albumToDelete);
        await deleter.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifier = fixture.CreateDatabase();
        Assert.False(await verifier.AlbumHasTracks.AnyAsync(l => l.AlbumId == album.Id, TestContext.Current.CancellationToken));
        Assert.True(await verifier.Tracks.AnyAsync(t => t.Id == track.Id, TestContext.Current.CancellationToken));
    }

    private static Track BuildTrack(User owner, string title)
    {
        return new Track
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            DurationSeconds = 100,
            OwnerUserId = owner.Id,
            Owner = owner,
            Pictures = new TrackPictures(),
            Audio = new TrackAudio()
        };
    }
}
