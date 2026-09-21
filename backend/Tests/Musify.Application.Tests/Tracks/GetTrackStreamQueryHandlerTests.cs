using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class GetTrackStreamQueryHandlerTests : HandlerTestBase
    {
        private readonly IStreamTicketService ticketService = Substitute.For<IStreamTicketService>();

        private GetTrackStreamQueryHandler CreateHandler(PlaybackConfiguration? playback = null)
        {
            ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("ticket-token", 60));

            var issuer = new TrackStreamIssuer(
                Database,
                ticketService,
                TestConfigurations.Track(),
                TestConfigurations.StreamGateway("https://stream.musify.test"),
                playback ?? TestConfigurations.Playback());

            return new GetTrackStreamQueryHandler(Database, issuer, playback ?? TestConfigurations.Playback(), NoOpLogger<GetTrackStreamQueryHandler>());
        }

        [Fact]
        public async Task Handle_ProcessedTrack_IssuesTicketAndRecordsListen()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(new GetTrackStreamQuery(track.Id, owner.Id), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("ticket-token", result.Value.Ticket);
            Assert.Equal(60, result.Value.ExpiresInSeconds);
            Assert.Contains("audio-folder", result.Value.ManifestUrl);

            var history = Assert.Single(await Database.ListeningHistories.ToListAsync(TestContext.Current.CancellationToken));
            Assert.Equal(owner.Id, history.UserId);
            Assert.Equal(track.Id, history.TrackId);
        }

        [Fact]
        public async Task Handle_TrackMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetTrackStreamQuery(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_AudioNotProcessedYet_ReturnsConflict()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner, audio: TestEntities.PendingAudio());
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(new GetTrackStreamQuery(track.Id, owner.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_AnonymousUser_AnonymousListeningDisabled_ReturnsUnauthorized()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var handler = CreateHandler(TestConfigurations.Playback(allowAnonymousListening: false));

            var result = await handler.Handle(new GetTrackStreamQuery(track.Id, UserId: null), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_AnonymousUser_AnonymousListeningEnabled_IssuesTicketWithoutRecordingHistory()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var handler = CreateHandler(TestConfigurations.Playback(allowAnonymousListening: true));

            var result = await handler.Handle(new GetTrackStreamQuery(track.Id, UserId: null), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("ticket-token", result.Value.Ticket);
            Assert.Empty(await Database.ListeningHistories.ToListAsync(TestContext.Current.CancellationToken));
        }
    }
}
