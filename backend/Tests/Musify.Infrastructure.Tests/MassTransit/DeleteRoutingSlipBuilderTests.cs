using Musify.Application.Configuration;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.RoutingSlip;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;
using Musify.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Musify.Infrastructure.Tests.MassTransit;

[Collection(InfrastructureCollection.Name)]
public sealed class DeleteRoutingSlipBuilderTests(InfrastructureTestFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BuildAlbum_WithProcessedPictures_RemovesEveryPictureBeforeDeletingTheRow()
    {
        var album = await SeedAlbumAsync(new EntityPictures
        {
            OriginalName = "original.webp", SmallName = "small.webp", MediumName = "medium.webp", LargeName = "large.webp"
        });
        await using var database = fixture.CreateDatabase();

        var slip = (await CreateBuilder(database).BuildAlbumAsync(album.Id, album.OwnerUserId, null, Ct)).Build();

        Assert.Equal(
            [
                ActivityNames.MarkAlbumAsRemoving,
                ActivityNames.RemoveAlbumOriginalPicture,
                ActivityNames.RemoveAlbumSmallPicture,
                ActivityNames.RemoveAlbumMediumPicture,
                ActivityNames.RemoveAlbumLargePicture,
                ActivityNames.DeleteAlbumFromDb,
            ],
            slip.Itinerary.Select(activity => activity.Name));
    }

    [Fact]
    public async Task BuildAlbum_WithPendingPicture_OnlyRemovesTheOriginal()
    {
        var album = await SeedAlbumAsync(EntityPictures.Pending("original.webp"));
        await using var database = fixture.CreateDatabase();

        var slip = (await CreateBuilder(database).BuildAlbumAsync(album.Id, album.OwnerUserId, null, Ct)).Build();

        Assert.Equal(
            [ActivityNames.MarkAlbumAsRemoving, ActivityNames.RemoveAlbumOriginalPicture, ActivityNames.DeleteAlbumFromDb],
            slip.Itinerary.Select(activity => activity.Name));
    }

    [Fact]
    public async Task BuildAlbum_MissingAlbum_OnlyMarksAndDeletes()
    {
        await using var database = fixture.CreateDatabase();

        var slip = (await CreateBuilder(database).BuildAlbumAsync(Guid.NewGuid(), 1, null, Ct)).Build();

        Assert.Equal(
            [ActivityNames.MarkAlbumAsRemoving, ActivityNames.DeleteAlbumFromDb],
            slip.Itinerary.Select(activity => activity.Name));
    }

    [Fact]
    public async Task BuildPlayList_WithoutPictures_OnlyMarksAndDeletes()
    {
        var playList = await SeedPlayListAsync(pictures: null);
        await using var database = fixture.CreateDatabase();

        var slip = (await CreateBuilder(database).BuildPlayListAsync(playList.Id, playList.OwnerUserId, null, Ct)).Build();

        Assert.Equal(
            [ActivityNames.MarkPlayListAsRemoving, ActivityNames.DeletePlayListFromDb],
            slip.Itinerary.Select(activity => activity.Name));
    }

    [Fact]
    public async Task BuildPlayList_WithPictures_RemovesThemBetweenMarkAndDelete()
    {
        var playList = await SeedPlayListAsync(new EntityPictures { OriginalName = "original.webp", SmallName = "small.webp" });
        await using var database = fixture.CreateDatabase();

        var slip = (await CreateBuilder(database).BuildPlayListAsync(playList.Id, playList.OwnerUserId, null, Ct)).Build();

        Assert.Equal(
            [
                ActivityNames.MarkPlayListAsRemoving,
                ActivityNames.RemovePlayListOriginalPicture,
                ActivityNames.RemovePlayListSmallPicture,
                ActivityNames.DeletePlayListFromDb,
            ],
            slip.Itinerary.Select(activity => activity.Name));
    }

    [Fact]
    public async Task BuildTrack_Unprocessed_NeverRemovesTheSharedPresetPictures()
    {
        var track = await SeedTrackAsync(new TrackPictures { OriginalName = "original.webp", SmallName = "preset.webp" }, new TrackAudio { OriginalName = "audio.mp3" });
        await using var database = fixture.CreateDatabase();

        var slip = (await CreateBuilder(database).BuildTrackAsync(track.Id, track.OwnerUserId, null, Ct)).Build();

        Assert.Equal(
            [
                ActivityNames.MarkTrackAsRemoving,
                ActivityNames.RemoveTrackOriginalPicture,
                ActivityNames.RemoveTrackOriginalAudio,
                ActivityNames.DeleteTrackFromDb,
            ],
            slip.Itinerary.Select(activity => activity.Name));
    }

    [Fact]
    public async Task BuildTrack_Processed_RemovesResizedPicturesAndProcessedAudio()
    {
        var track = await SeedTrackAsync(
            new TrackPictures
            {
                OriginalName = "original.webp", SmallName = "s.webp", MediumName = "m.webp", LargeName = "l.webp",
                ProcessingStatus = ProcessingStatus.Completed
            },
            new TrackAudio { OriginalName = "audio.mp3", FolderName = "folder", TranscodeStatus = ProcessingStatus.Completed });
        await using var database = fixture.CreateDatabase();

        var slip = (await CreateBuilder(database).BuildTrackAsync(track.Id, track.OwnerUserId, null, Ct)).Build();

        Assert.Equal(
            [
                ActivityNames.MarkTrackAsRemoving,
                ActivityNames.RemoveTrackOriginalPicture,
                ActivityNames.RemoveTrackSmallPicture,
                ActivityNames.RemoveTrackMediumPicture,
                ActivityNames.RemoveTrackLargePicture,
                ActivityNames.RemoveTrackOriginalAudio,
                ActivityNames.RemoveTrackProcessedAudio,
                ActivityNames.DeleteTrackFromDb,
            ],
            slip.Itinerary.Select(activity => activity.Name));
    }

    private static DeleteRoutingSlipBuilder CreateBuilder(Musify.Application.Contracts.IDatabase database) =>
        new(
            database,
            new TrackConfiguration(),
            new AlbumConfiguration(),
            new PlayListConfiguration(),
            new ApplicationStorageConfiguration { Bucket = "bucket" });

    private async Task<User> SeedUserAsync()
    {
        await using var database = fixture.CreateDatabase();
        var name = $"delete-{Guid.NewGuid():N}"[..30];
        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = name, NormalizedName = name.ToUpperInvariant() };
        await database.Users.AddAsync(user, Ct);
        await database.SaveChangesAsync(Ct);
        return user;
    }

    private async Task<Album> SeedAlbumAsync(EntityPictures? pictures)
    {
        var user = await SeedUserAsync();
        await using var database = fixture.CreateDatabase();
        var title = $"album-{Guid.NewGuid():N}";
        var album = new Album { Title = title, NormalizedTitle = title.ToUpperInvariant(), OwnerUserId = user.Id, Pictures = pictures };
        await database.Albums.AddAsync(album, Ct);
        await database.SaveChangesAsync(Ct);
        return album;
    }

    private async Task<PlayList> SeedPlayListAsync(EntityPictures? pictures)
    {
        var user = await SeedUserAsync();
        await using var database = fixture.CreateDatabase();
        var name = $"pl-{Guid.NewGuid():N}"[..30];
        var playList = new PlayList { Name = name, NormalizedName = name.ToUpperInvariant(), OwnerUserId = user.Id, Pictures = pictures };
        await database.PlayLists.AddAsync(playList, Ct);
        await database.SaveChangesAsync(Ct);
        return playList;
    }

    private async Task<Track> SeedTrackAsync(TrackPictures pictures, TrackAudio audio)
    {
        var user = await SeedUserAsync();
        await using var database = fixture.CreateDatabase();
        var title = $"track-{Guid.NewGuid():N}";
        var track = new Track
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            DurationSeconds = 10,
            OwnerUserId = user.Id,
            Pictures = pictures,
            Audio = audio
        };
        await database.Tracks.AddAsync(track, Ct);
        await database.SaveChangesAsync(Ct);
        return track;
    }
}
