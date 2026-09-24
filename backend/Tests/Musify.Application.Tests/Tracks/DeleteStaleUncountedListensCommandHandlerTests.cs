using Microsoft.EntityFrameworkCore;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class DeleteStaleUncountedListensCommandHandlerTests : HandlerTestBase
{
    private DeleteStaleUncountedListensCommandHandler CreateHandler() =>
        new(Database, NoOpLogger<DeleteStaleUncountedListensCommandHandler>());

    [Fact]
    public async Task Handle_DeletesOnlyOldUncountedListens()
    {
        var owner = TestEntities.User();
        var track = TestEntities.Track(owner);
        var oldUncounted = TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddDays(-9), playedSeconds: 2, isCounted: false);
        var oldCounted = TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddDays(-9), playedSeconds: 100);
        var recentUncounted = TestEntities.ListeningHistory(owner.Id, track.Id, DateTime.UtcNow.AddDays(-2), playedSeconds: 2, isCounted: false);
        await SeedAsync(owner, track, oldUncounted, oldCounted, recentUncounted);

        var deleted = await CreateHandler().Handle(new DeleteStaleUncountedListensCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(1, deleted);
        var remaining = await Database.ListeningHistories.Select(l => l.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(oldUncounted.Id, remaining);
        Assert.Contains(oldCounted.Id, remaining);
        Assert.Contains(recentUncounted.Id, remaining);
    }
}
