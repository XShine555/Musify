using Microsoft.EntityFrameworkCore;
using Musify.Application.Mixes;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Mixes;

public sealed class GenerateMixesForUserCommandHandlerTests : HandlerTestBase
{
    private GenerateMixesForUserCommandHandler CreateHandler() => new(
        Database, TestConfigurations.Mix(), NoOpLogger<GenerateMixesForUserCommandHandler>());

    [Fact]
    public async Task Handle_NoTracks_DoesNotCreateAnyMix()
    {
        var user = TestEntities.User();
        await SeedAsync(user);

        await CreateHandler().Handle(new GenerateMixesForUserCommand(user.Id), TestContext.Current.CancellationToken);
        Assert.Empty(await Database.Mixes.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_AllTracksAlreadyListenedTo_CreatesOnlyTheDailyMix()
    {
        var user = TestEntities.User();
        var track = TestEntities.Track(user);
        await SeedAsync(
            user, track,
            TestEntities.ListeningHistory(user.Id, track.Id));

        await CreateHandler().Handle(new GenerateMixesForUserCommand(user.Id), TestContext.Current.CancellationToken);
        var mixes = await Database.Mixes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(mixes);
        Assert.Equal(MixKind.Daily, mixes[0].Kind);
    }

    [Fact]
    public async Task Handle_UnheardTracksInLibrary_CreatesDiscoveryAndDailyMixes_AndReplacesPreviousMixes()
    {
        var user = TestEntities.User();
        var listenedTrack = TestEntities.Track(user, "Listened Track");
        var unheardTrack = TestEntities.Track(user, "Unheard Track");

        var staleMix = TestEntities.Mix(user.Id, MixKind.Daily);
        var staleItem = TestEntities.MixItem(staleMix.Id, listenedTrack.Id);

        await SeedAsync(
            user, listenedTrack, unheardTrack, staleMix, staleItem,
            TestEntities.ListeningHistory(user.Id, listenedTrack.Id));

        await CreateHandler().Handle(new GenerateMixesForUserCommand(user.Id), TestContext.Current.CancellationToken);
        var mixes = await Database.Mixes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(mixes, mix => mix.Id == staleMix.Id);
        Assert.Contains(mixes, mix => mix.Kind == MixKind.Discovery);
        Assert.Contains(mixes, mix => mix.Kind == MixKind.Daily);
        Assert.Empty(await Database.MixItems.Where(item => item.MixId == staleMix.Id).ToListAsync(TestContext.Current.CancellationToken));

        var discoveryMix = mixes.Single(mix => mix.Kind == MixKind.Discovery);
        var discoveryItems = await Database.MixItems
            .Where(item => item.MixId == discoveryMix.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.All(discoveryItems, item => Assert.Equal(unheardTrack.Id, item.TrackId));
    }

    [Fact]
    public async Task Handle_TrackWithOnlyUncountedListens_IsTreatedAsUnheard()
    {
        var user = TestEntities.User();
        var skipped = TestEntities.Track(user, "Only skipped");
        await SeedAsync(
            user, skipped,
            TestEntities.ListeningHistory(user.Id, skipped.Id, playedSeconds: 3, isCounted: false));

        await CreateHandler().Handle(new GenerateMixesForUserCommand(user.Id), TestContext.Current.CancellationToken);

        var mixes = await Database.Mixes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(mixes, mix => mix.Kind == MixKind.Discovery);
    }
}
