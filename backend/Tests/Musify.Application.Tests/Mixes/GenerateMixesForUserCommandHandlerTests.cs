using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Mixes;

public sealed class GenerateMixesForUserCommandHandlerTests : HandlerTestBase
{
    private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

    private GenerateMixesForUserCommandHandler CreateHandler() => new(
        Database, youTubeMusicService, TestConfigurations.Mix(), NoOpLogger<GenerateMixesForUserCommandHandler>());

    [Fact]
    public async Task Handle_NoListeningHistory_DoesNotCreateAnyMix()
    {
        var user = TestEntities.User();
        await SeedAsync(user);

        var result = await CreateHandler().Handle(new GenerateMixesForUserCommand(user.Id), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(await Database.Mixes.ToListAsync());
    }

    [Fact]
    public async Task Handle_HistoryWithNoKnownArtists_DoesNotCreateAnyMix()
    {
        var user = TestEntities.User();
        var track = TestEntities.LocalTrack(user);
        await SeedAsync(user, track, TestEntities.ListeningHistory(user.Id, track.Id));

        var result = await CreateHandler().Handle(new GenerateMixesForUserCommand(user.Id), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(await Database.Mixes.ToListAsync());
    }

    [Fact]
    public async Task Handle_SeedArtistWithYouTubeAndLocalCandidates_ReplacesPreviousMixes()
    {
        youTubeMusicService
            .SearchSongsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new YouTubeSearchResult(
                [new YouTubeSongResult("yt-1", "YouTube Song", "Seed Artist", "Album", 200, "https://img/thumb.jpg", IsExplicit: false, [] )],
                ContinuationToken: string.Empty));
        youTubeMusicService.ResolveArtworkUrl(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        var user = TestEntities.User();
        var artist = TestEntities.Artist("Seed Artist");
        var listenedTrack = TestEntities.ExternalTrack(externalId: "listened", title: "Listened Track");
        var otherTrackByArtist = TestEntities.ExternalTrack(externalId: "not-listened", title: "Other Track");

        var staleMix = TestEntities.Mix(user.Id, "Stale Mix");
        var staleItem = TestEntities.MixItem(staleMix.Id, title: "Stale Item");

        await SeedAsync(
            user, artist, listenedTrack, otherTrackByArtist, staleMix, staleItem,
            new TrackArtist { TrackId = listenedTrack.Id, ArtistId = artist.Id, Position = 0 },
            new TrackArtist { TrackId = otherTrackByArtist.Id, ArtistId = artist.Id, Position = 0 },
            TestEntities.ListeningHistory(user.Id, listenedTrack.Id));

        var result = await CreateHandler().Handle(new GenerateMixesForUserCommand(user.Id), CancellationToken.None);

        Assert.False(result.IsError);
        var mixes = await Database.Mixes.ToListAsync();
        Assert.NotEmpty(mixes);
        Assert.DoesNotContain(mixes, mix => mix.Title == "Stale Mix");
        Assert.Empty(await Database.MixItems.Where(item => item.MixId == staleMix.Id).ToListAsync());
    }
}
