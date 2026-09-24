using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Likes;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Likes;

public sealed class ToggleTrackLikeCommandHandlerTests : HandlerTestBase
{
    private ToggleTrackLikeCommandHandler CreateHandler() =>
        new(Database, NoOpLogger<ToggleTrackLikeCommandHandler>());

    [Fact]
    public async Task Handle_TrackNotLiked_LikesIt()
    {
        var owner = TestEntities.User();
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        var result = await CreateHandler().Handle(
            new ToggleTrackLikeCommand(owner.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        Assert.Single(await Database.TrackLikes.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TrackAlreadyLiked_UnlikesIt()
    {
        var owner = TestEntities.User();
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track, new Musify.Domain.Entities.TrackLike { UserId = owner.Id, TrackId = track.Id });

        var result = await CreateHandler().Handle(
            new ToggleTrackLikeCommand(owner.Id, track.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.False(result.Value);
        Assert.Empty(await Database.TrackLikes.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TrackMissing_ReturnsNotFound()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner);

        var result = await CreateHandler().Handle(
            new ToggleTrackLikeCommand(owner.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
