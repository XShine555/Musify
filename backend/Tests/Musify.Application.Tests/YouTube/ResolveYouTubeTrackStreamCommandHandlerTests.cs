using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.YouTube;
using Musify.Application.YouTube.Responses;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.YouTube
{
    public sealed class ResolveYouTubeTrackStreamCommandHandlerTests : HandlerTestBase
    {
        private readonly IYouTubeMusicService youTubeMusicService = Substitute.For<IYouTubeMusicService>();
        private readonly IStreamTicketService ticketService = Substitute.For<IStreamTicketService>();

        private ResolveYouTubeTrackStreamCommandHandler CreateHandler(PlaybackConfiguration? playback = null)
        {
            playback ??= TestConfigurations.Playback();
            var provisioner = new YouTubeTrackProvisioner(
                Database, youTubeMusicService, TestConfigurations.Track(), NoOpLogger<YouTubeTrackProvisioner>());
            var issuer = new TrackStreamIssuer(
                Database, ticketService, TestConfigurations.Track(), TestConfigurations.StreamGateway(), playback);

            return new ResolveYouTubeTrackStreamCommandHandler(
                Database, youTubeMusicService, provisioner, issuer, playback, NoOpLogger<ResolveYouTubeTrackStreamCommandHandler>());
        }

        [Fact]
        public async Task Handle_AlreadyProcessedTrack_IssuesServerStreamWithoutCallingYouTubeForAudio()
        {
            ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("token", 60));

            var user = TestEntities.User(1);
            var track = TestEntities.ExternalTrack(externalId: "abc123");
            await SeedAsync(user, track);

            var result = await CreateHandler().Handle(new ResolveYouTubeTrackStreamCommand("abc123", 1), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(YouTubeStreamMode.Server, result.Value.Mode);
            Assert.Equal("token", result.Value.Ticket);
            await youTubeMusicService.DidNotReceiveWithAnyArgs().GetAudioStreamAsync(default!, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Handle_UnknownTrack_ProvisionsItAndRecordsListeningHistory()
        {
            youTubeMusicService.GetAudioStreamAsync("new-video", Arg.Any<CancellationToken>())
                .Returns(new YouTubeStreamInfo("https://youtube.example/stream", 300));
            youTubeMusicService.GetSongAsync("new-video", Arg.Any<CancellationToken>())
                .Returns(new YouTubeSongResult("new-video", "Title", "Artist", "Album", 200, "https://img/thumb.jpg", false, []));
            youTubeMusicService.ResolveArtworkUrl(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
            await SeedAsync(TestEntities.User(42));

            var result = await CreateHandler().Handle(new ResolveYouTubeTrackStreamCommand("new-video", 42), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(YouTubeStreamMode.YouTube, result.Value.Mode);
            Assert.Equal("https://youtube.example/stream", result.Value.StreamUrl);
            Assert.NotNull(result.Value.TrackId);

            var history = Assert.Single(await Database.ListeningHistories.ToListAsync(TestContext.Current.CancellationToken));
            Assert.Equal(42, history.UserId);
        }

        [Fact]
        public async Task Handle_AnonymousUser_AnonymousListeningDisabled_ReturnsUnauthorized()
        {
            var handler = CreateHandler(TestConfigurations.Playback(allowAnonymousListening: false));

            var result = await handler.Handle(new ResolveYouTubeTrackStreamCommand("abc123", UserId: null), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
            await youTubeMusicService.DidNotReceiveWithAnyArgs().GetAudioStreamAsync(default!, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Handle_AnonymousUser_AlreadyProcessedTrack_IssuesServerStreamWithoutRecordingHistory()
        {
            ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("token", 60));
            var track = TestEntities.ExternalTrack(externalId: "abc123");
            await SeedAsync(track);

            var handler = CreateHandler(TestConfigurations.Playback(allowAnonymousListening: true));

            var result = await handler.Handle(new ResolveYouTubeTrackStreamCommand("abc123", UserId: null), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(YouTubeStreamMode.Server, result.Value.Mode);
            Assert.Empty(await Database.ListeningHistories.ToListAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_AnonymousUser_FragmentCapConfigured_UnprovisionedTrack_ReturnsConflictWithoutCallingYouTube()
        {
            var handler = CreateHandler(TestConfigurations.Playback(allowAnonymousListening: true, anonymousFragmentSeconds: 30));

            var result = await handler.Handle(new ResolveYouTubeTrackStreamCommand("not-provisioned-yet", UserId: null), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
            await youTubeMusicService.DidNotReceiveWithAnyArgs().GetAudioStreamAsync(default!, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Handle_AnonymousUser_NoFragmentCap_UnprovisionedTrack_FallsBackToDirectYouTubeStream()
        {
            youTubeMusicService.GetAudioStreamAsync("new-video", Arg.Any<CancellationToken>())
                .Returns(new YouTubeStreamInfo("https://youtube.example/stream", 300));
            youTubeMusicService.GetSongAsync("new-video", Arg.Any<CancellationToken>())
                .Returns(new YouTubeSongResult("new-video", "Title", "Artist", "Album", 200, "https://img/thumb.jpg", false, []));
            youTubeMusicService.ResolveArtworkUrl(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

            var handler = CreateHandler(TestConfigurations.Playback(allowAnonymousListening: true, anonymousFragmentSeconds: 0));

            var result = await handler.Handle(new ResolveYouTubeTrackStreamCommand("new-video", UserId: null), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(YouTubeStreamMode.YouTube, result.Value.Mode);
            Assert.Empty(await Database.ListeningHistories.ToListAsync(TestContext.Current.CancellationToken));
        }
    }
}
