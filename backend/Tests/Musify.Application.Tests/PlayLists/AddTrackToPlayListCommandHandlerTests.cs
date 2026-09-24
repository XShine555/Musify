using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class AddTrackToPlayListCommandHandlerTests : HandlerTestBase
{
    private AddTrackToPlayListCommandHandler CreateHandler() => new(Database, NoOpLogger<AddTrackToPlayListCommandHandler>());

    [Fact]
    public async Task Handle_FirstTrack_AddsItAtPositionZero()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, playList, track);

        var result = await CreateHandler().Handle(new AddTrackToPlayListCommand(owner.Id, playList.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var link = Assert.Single(await Database.PlayListHasTracks.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, link.Position);
    }

    [Fact]
    public async Task Handle_PlayListAlreadyHasTracks_AppendsAtNextPosition()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var existingTrack = TestEntities.Track(owner, "Existing");
        var newTrack = TestEntities.Track(owner, "New");
        await SeedAsync(
            owner, playList, existingTrack, newTrack,
            new PlayListHasTrack { PlayListId = playList.Id, TrackId = existingTrack.Id, Position = 0 });

        var result = await CreateHandler().Handle(new AddTrackToPlayListCommand(owner.Id, playList.Id, newTrack.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var newLink = await Database.PlayListHasTracks.SingleAsync(link => link.TrackId == newTrack.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, newLink.Position);
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new AddTrackToPlayListCommand(1, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var result = await CreateHandler().Handle(new AddTrackToPlayListCommand(stranger.Id, playList.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackMissing_ReturnsNotFound()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var result = await CreateHandler().Handle(new AddTrackToPlayListCommand(owner.Id, playList.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackAlreadyInPlayList_ReturnsConflict()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });

        var result = await CreateHandler().Handle(new AddTrackToPlayListCommand(owner.Id, playList.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }
}
