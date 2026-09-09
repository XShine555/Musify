using ErrorOr;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class GetPlayListByIdQueryHandlerTests : HandlerTestBase
{
    private GetPlayListByIdQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_ExistingPlayList_ReturnsItWithCoverTrackIds()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.LocalTrack(owner);
        await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });

        var result = await CreateHandler().Handle(new GetPlayListByIdQuery(playList.Id), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(playList.Name, result.Value.Name);
        Assert.Equal([track.Id], result.Value.CoverTrackIds);
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new GetPlayListByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
