using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class AddTracksToPlayListCommandHandlerTests : HandlerTestBase
{
    private AddTracksToPlayListCommandHandler CreateHandler() => new(Database, NoOpLogger<AddTracksToPlayListCommandHandler>());

    [Fact]
    public async Task Handle_FirstTrack_AddsItAtPositionZero()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, playList, track);

        var result = await CreateHandler().Handle(new AddTracksToPlayListCommand(owner.Id, playList.Id, [track.Id]), TestContext.Current.CancellationToken);

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

        var result = await CreateHandler().Handle(new AddTracksToPlayListCommand(owner.Id, playList.Id, [newTrack.Id]), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var newLink = await Database.PlayListHasTracks.SingleAsync(link => link.TrackId == newTrack.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, newLink.Position);
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new AddTracksToPlayListCommand(1, Guid.NewGuid(), [Guid.NewGuid()]), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var result = await CreateHandler().Handle(new AddTracksToPlayListCommand(stranger.Id, playList.Id, [Guid.NewGuid()]), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackMissing_ReturnsNotFound()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var result = await CreateHandler().Handle(new AddTracksToPlayListCommand(owner.Id, playList.Id, [Guid.NewGuid()]), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackAlreadyInPlayList_IsSkipped()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });

        var result = await CreateHandler().Handle(new AddTracksToPlayListCommand(owner.Id, playList.Id, [track.Id]), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Single(await Database.PlayListHasTracks.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TrySaveChangesAsync_DuplicatePlayListTrack_MapsTheUniqueViolationToTheGivenError()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });
        Database.PlayListHasTracks.Add(new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 1 });

        var result = await Database.TrySaveChangesAsync(Error.Conflict("Test.Duplicate", "duplicate"), TestContext.Current.CancellationToken);

        Assert.Equal("Test.Duplicate", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_SeveralTracks_AppendsThemInOrderSkippingExistingOnes()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var existing = TestEntities.Track(owner, "Existing");
        var first = TestEntities.Track(owner, "First");
        var second = TestEntities.Track(owner, "Second");
        await SeedAsync(
            owner, playList, existing, first, second,
            new PlayListHasTrack { PlayListId = playList.Id, TrackId = existing.Id, Position = 0 });

        var result = await CreateHandler().Handle(
            new AddTracksToPlayListCommand(owner.Id, playList.Id, [first.Id, existing.Id, second.Id, first.Id]),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var links = await Database.PlayListHasTracks.OrderBy(link => link.Position).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([existing.Id, first.Id, second.Id], links.Select(link => link.TrackId));
        Assert.Equal([0, 1, 2], links.Select(link => link.Position));
    }
}
