using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class GetTracksByUserIdQueryHandlerTests : HandlerTestBase
{
    private GetTracksByUserIdQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_ReturnsOnlyTracksLinkedToThatUser()
    {
        var owner = TestEntities.User(1, "owner");
        var other = TestEntities.User(2, "other");
        var mine = TestEntities.Track(owner, "Mine");
        var theirs = TestEntities.Track(other, "Theirs");
        await SeedAsync(
            owner, other, mine, theirs);

        var result = await CreateHandler().Handle(new GetTracksByUserIdQuery(owner.Id, Name: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var track = Assert.Single(result.Value.Items);
        Assert.Equal("Mine", track.Title);
    }
}
