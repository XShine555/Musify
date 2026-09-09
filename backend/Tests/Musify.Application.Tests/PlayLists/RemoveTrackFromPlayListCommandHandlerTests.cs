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
        var track = TestEntities.LocalTrack(owner);
        await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });

        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(owner.Id, playList.Id, track.Id), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(Database.PlayListHasTracks);
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(1, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(stranger.Id, playList.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackNotLinked_ReturnsNotFound()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var result = await CreateHandler().Handle(new RemoveTrackFromPlayListCommand(owner.Id, playList.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
