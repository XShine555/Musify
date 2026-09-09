using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class GetListeningHistoryQueryHandlerTests : HandlerTestBase
{
    private GetListeningHistoryQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_MultipleListens_ReturnsDistinctTracksMostRecentFirst()
    {
        var owner = TestEntities.User();
        var older = TestEntities.LocalTrack(owner, "Older listen");
        var newer = TestEntities.LocalTrack(owner, "Newer listen");
        await SeedAsync(
            owner, older, newer,
            TestEntities.ListeningHistory(owner.Id, older.Id, DateTime.UtcNow.AddMinutes(-10)),
            TestEntities.ListeningHistory(owner.Id, older.Id, DateTime.UtcNow.AddMinutes(-5)),
            TestEntities.ListeningHistory(owner.Id, newer.Id, DateTime.UtcNow));

        var result = await CreateHandler().Handle(new GetListeningHistoryQuery(owner.Id), CancellationToken.None);

        Assert.Equal(["Newer listen", "Older listen"], result.Select(track => track.Title));
    }

    [Fact]
    public async Task Handle_NoHistory_ReturnsEmpty()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner);

        var result = await CreateHandler().Handle(new GetListeningHistoryQuery(owner.Id), CancellationToken.None);

        Assert.Empty(result);
    }
}
