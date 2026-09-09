using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.PlayLists;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class AddYouTubeTrackToPlayListCommandHandlerTests : HandlerTestBase
{
    private readonly IEventBus eventBus = Substitute.For<IEventBus>();
    private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();

    private AddYouTubeTrackToPlayListCommandHandler CreateHandler()
    {
        var provisioner = new YouTubeTrackProvisioner(
            Database, youTubeMusicService, TestConfigurations.Track(), NoOpLogger<YouTubeTrackProvisioner>());

        return new AddYouTubeTrackToPlayListCommandHandler(
            Database,
            eventBus,
            youTubeMusicService,
            provisioner,
            TestConfigurations.Storage(),
            TestConfigurations.Track(),
            NoOpLogger<AddYouTubeTrackToPlayListCommandHandler>());
    }

    private static YouTubeSongResult BuildSong(string videoId) => new(
        videoId, "Song Title", "Song Artist", "Song Album", 200, "https://img.example/thumb.jpg", false,
        [new YouTubeArtistRef("artist-id", "Song Artist")]);

    [Fact]
    public async Task Handle_TrackAlreadyProcessedAndNotLinked_AddsItWithoutQueuingDownload()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.ExternalTrack(externalId: "abc123");
        await SeedAsync(owner, playList, track);

        var result = await CreateHandler().Handle(new AddYouTubeTrackToPlayListCommand(owner.Id, playList.Id, "abc123"), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Single(await Database.PlayListHasTracks.ToListAsync(TestContext.Current.CancellationToken));
        await eventBus.DidNotReceive().PublishAsync(Arg.Any<Musify.Application.Events.DownloadYouTubeTrackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownVideo_ProvisionsTrackAndQueuesDownload()
    {
        youTubeMusicService.GetSongAsync("new-video", Arg.Any<CancellationToken>()).Returns(BuildSong("new-video"));
        youTubeMusicService.ResolveArtworkUrl(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var result = await CreateHandler().Handle(new AddYouTubeTrackToPlayListCommand(owner.Id, playList.Id, "new-video"), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("Song Title", result.Value.Title);
        var track = await Database.ExternalTracks.SingleAsync(t => t.ExternalId == "new-video", TestContext.Current.CancellationToken);
        Assert.True(track.Audio.DownloadRequested);
        await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.DownloadYouTubeTrackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new AddYouTubeTrackToPlayListCommand(1, Guid.NewGuid(), "abc123"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var result = await CreateHandler().Handle(new AddYouTubeTrackToPlayListCommand(stranger.Id, playList.Id, "abc123"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_TrackAlreadyLinked_ReturnsConflict()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        var track = TestEntities.ExternalTrack(externalId: "abc123");
        await SeedAsync(owner, playList, track, new PlayListHasTrack { PlayListId = playList.Id, TrackId = track.Id, Position = 0 });

        var result = await CreateHandler().Handle(new AddYouTubeTrackToPlayListCommand(owner.Id, playList.Id, "abc123"), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }
}
