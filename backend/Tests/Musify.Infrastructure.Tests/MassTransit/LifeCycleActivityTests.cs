using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Activities.LifeCycle;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Infrastructure.Tests.MassTransit;

[Collection(InfrastructureCollection.Name)]
public sealed class LifeCycleActivityTests(InfrastructureTestFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MarkTrack_ExistingTrack_SetsTheStatus()
    {
        var track = await SeedTrackAsync();
        await using var database = fixture.CreateDatabase();
        var activity = new MarkTrackLifeCycleActivity(database, NullLogger<MarkTrackLifeCycleActivity>.Instance);
        var context = ExecuteContextFor(new MarkLifeCycleArguments(track.Id, LifeCycleStatus.Removing));

        await activity.Execute(context);

        context.Received(1).Completed();
        await using var verifier = fixture.CreateDatabase();
        var stored = await verifier.Tracks.AsNoTracking().SingleAsync(t => t.Id == track.Id, Ct);
        Assert.Equal(LifeCycleStatus.Removing, stored.LifeCycleStatus);
    }

    [Fact]
    public async Task MarkAlbum_ExistingAlbum_SetsTheStatus()
    {
        var album = await SeedAlbumAsync();
        await using var database = fixture.CreateDatabase();
        var activity = new MarkAlbumLifeCycleActivity(database, NullLogger<MarkAlbumLifeCycleActivity>.Instance);

        await activity.Execute(ExecuteContextFor(new MarkLifeCycleArguments(album.Id, LifeCycleStatus.Failed)));

        await using var verifier = fixture.CreateDatabase();
        Assert.Equal(LifeCycleStatus.Failed, (await verifier.Albums.AsNoTracking().SingleAsync(a => a.Id == album.Id, Ct)).LifeCycleStatus);
    }

    [Fact]
    public async Task MarkPlayList_ExistingPlayList_SetsTheStatus()
    {
        var playList = await SeedPlayListAsync();
        await using var database = fixture.CreateDatabase();
        var activity = new MarkPlayListLifeCycleActivity(database, NullLogger<MarkPlayListLifeCycleActivity>.Instance);

        await activity.Execute(ExecuteContextFor(new MarkLifeCycleArguments(playList.Id, LifeCycleStatus.Removing)));

        await using var verifier = fixture.CreateDatabase();
        Assert.Equal(LifeCycleStatus.Removing, (await verifier.PlayLists.AsNoTracking().SingleAsync(p => p.Id == playList.Id, Ct)).LifeCycleStatus);
    }

    [Fact]
    public async Task Mark_MissingEntity_CompletesWithoutFailing()
    {
        await using var database = fixture.CreateDatabase();
        var activity = new MarkTrackLifeCycleActivity(database, NullLogger<MarkTrackLifeCycleActivity>.Instance);
        var context = ExecuteContextFor(new MarkLifeCycleArguments(Guid.NewGuid(), LifeCycleStatus.Failed));

        await activity.Execute(context);

        context.Received(1).Completed();
    }

    [Fact]
    public async Task DeleteTrack_ExistingTrack_RemovesTheRow()
    {
        var track = await SeedTrackAsync();
        await using var database = fixture.CreateDatabase();
        var activity = new DeleteTrackActivity(database, NullLogger<DeleteTrackActivity>.Instance);
        var context = ExecuteContextFor(new DeleteEntityArguments(track.Id));

        await activity.Execute(context);

        context.Received(1).Completed();
        await using var verifier = fixture.CreateDatabase();
        Assert.False(await verifier.Tracks.AnyAsync(t => t.Id == track.Id, Ct));
    }

    [Fact]
    public async Task DeleteAlbum_ExistingAlbum_RemovesTheRow()
    {
        var album = await SeedAlbumAsync();
        await using var database = fixture.CreateDatabase();
        var activity = new DeleteAlbumActivity(database, NullLogger<DeleteAlbumActivity>.Instance);

        await activity.Execute(ExecuteContextFor(new DeleteEntityArguments(album.Id)));

        await using var verifier = fixture.CreateDatabase();
        Assert.False(await verifier.Albums.AnyAsync(a => a.Id == album.Id, Ct));
    }

    [Fact]
    public async Task DeletePlayList_ExistingPlayList_RemovesTheRow()
    {
        var playList = await SeedPlayListAsync();
        await using var database = fixture.CreateDatabase();
        var activity = new DeletePlayListActivity(database, NullLogger<DeletePlayListActivity>.Instance);

        await activity.Execute(ExecuteContextFor(new DeleteEntityArguments(playList.Id)));

        await using var verifier = fixture.CreateDatabase();
        Assert.False(await verifier.PlayLists.AnyAsync(p => p.Id == playList.Id, Ct));
    }

    [Fact]
    public async Task Delete_MissingEntity_CompletesWithoutFailing()
    {
        await using var database = fixture.CreateDatabase();
        var activity = new DeleteTrackActivity(database, NullLogger<DeleteTrackActivity>.Instance);
        var context = ExecuteContextFor(new DeleteEntityArguments(Guid.NewGuid()));

        await activity.Execute(context);

        context.Received(1).Completed();
    }

    private static ExecuteContext<TArguments> ExecuteContextFor<TArguments>(TArguments arguments)
        where TArguments : class
    {
        var context = Substitute.For<ExecuteContext<TArguments>>();
        context.Arguments.Returns(arguments);
        context.CancellationToken.Returns(Ct);
        return context;
    }

    private async Task<User> SeedUserAsync()
    {
        await using var database = fixture.CreateDatabase();
        var name = $"lifecycle-{Guid.NewGuid():N}"[..30];
        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = name, NormalizedName = name.ToUpperInvariant() };
        await database.Users.AddAsync(user, Ct);
        await database.SaveChangesAsync(Ct);
        return user;
    }

    private async Task<Track> SeedTrackAsync()
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
            Pictures = new TrackPictures(),
            Audio = new TrackAudio()
        };
        await database.Tracks.AddAsync(track, Ct);
        await database.SaveChangesAsync(Ct);
        return track;
    }

    private async Task<Album> SeedAlbumAsync()
    {
        var user = await SeedUserAsync();
        await using var database = fixture.CreateDatabase();
        var title = $"album-{Guid.NewGuid():N}";
        var album = new Album { Title = title, NormalizedTitle = title.ToUpperInvariant(), OwnerUserId = user.Id };
        await database.Albums.AddAsync(album, Ct);
        await database.SaveChangesAsync(Ct);
        return album;
    }

    private async Task<PlayList> SeedPlayListAsync()
    {
        var user = await SeedUserAsync();
        await using var database = fixture.CreateDatabase();
        var name = $"pl-{Guid.NewGuid():N}"[..30];
        var playList = new PlayList { Name = name, NormalizedName = name.ToUpperInvariant(), OwnerUserId = user.Id };
        await database.PlayLists.AddAsync(playList, Ct);
        await database.SaveChangesAsync(Ct);
        return playList;
    }
}
