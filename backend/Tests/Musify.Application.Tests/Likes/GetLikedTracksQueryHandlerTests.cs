using Musify.Application.Likes;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Likes;

public sealed class GetLikedTracksQueryHandlerTests : HandlerTestBase
{
    private GetLikedTracksQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_MultipleLikes_ReturnsMostRecentFirst()
    {
        var owner = TestEntities.User();
        var older = TestEntities.Track(owner, "Older like");
        var newer = TestEntities.Track(owner, "Newer like");
        await SeedAsync(
            owner, older, newer,
            new TrackLike { UserId = owner.Id, TrackId = older.Id, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new TrackLike { UserId = owner.Id, TrackId = newer.Id, CreatedAt = DateTime.UtcNow });

        var result = await CreateHandler().Handle(new GetLikedTracksQuery(owner.Id, 1, 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(["Newer like", "Older like"], result.Value.Items.Select(track => track.Title));
    }

    [Fact]
    public async Task Handle_NoLikes_ReturnsEmpty()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner);

        var result = await CreateHandler().Handle(new GetLikedTracksQuery(owner.Id, 1, 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_OtherUsersLikes_AreExcluded()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, stranger, track, new TrackLike { UserId = stranger.Id, TrackId = track.Id });

        var result = await CreateHandler().Handle(new GetLikedTracksQuery(owner.Id, 1, 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Empty(result.Value.Items);
    }
}
