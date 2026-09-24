using ErrorOr;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class RemoveTrackFromPlayListCommandHandlerTests : HandlerTestBase
{
    private RemoveTrackFromPlayListCommandHandler CreateHandler() => new(Database, NoOpLogger<RemoveTrackFromPlayListCommandHandler>());

    [Fact]
    public async Task Handle_TrackInPlayList_RemovesTheLink()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });

        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(owner.Id, playList.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Empty(Database.PlayListHasTracks);
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(1, Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(stranger.Id, playList.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackNotLinked_ReturnsNotFound()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(owner.Id, playList.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackInTheMiddle_RenumbersTheRemainingTracks()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var first = TestEntities.Track(owner, "First");
        var second = TestEntities.Track(owner, "Second");
        var third = TestEntities.Track(owner, "Third");
        await SeedAsync(
            owner, playList, first, second, third,
            new PlayListHasTrack { PlayListId = playList.Id, TrackId = first.Id, Position = 0 },
            new PlayListHasTrack { PlayListId = playList.Id, TrackId = second.Id, Position = 1 },
            new PlayListHasTrack { PlayListId = playList.Id, TrackId = third.Id, Position = 2 });

        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(owner.Id, playList.Id, second.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var remaining = Database.PlayListHasTracks.OrderBy(link => link.Position).ToList();
        Assert.Equal([first.Id, third.Id], remaining.Select(link => link.TrackId));
        Assert.Equal([0, 1], remaining.Select(link => link.Position));
    }
}
