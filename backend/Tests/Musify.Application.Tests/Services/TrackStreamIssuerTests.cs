using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Services
{
    public sealed class TrackStreamIssuerTests
    {
        private readonly IStreamTicketService ticketService = Substitute.For<IStreamTicketService>();

        public TrackStreamIssuerTests() =>
            ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("signed-token", 120));

        private TrackStreamIssuer CreateIssuer(PlaybackConfiguration? playback = null) => new(
            ticketService,
            TestConfigurations.Track(),
            TestConfigurations.StreamGateway("https://stream.musify.test/"),
            playback ?? TestConfigurations.Playback());

        [Fact]
        public void Issue_BuildsManifestUrlAndCarriesTheListenId()
        {
            var listenId = Guid.NewGuid();

            var response = CreateIssuer().Issue("folder-42", userId: 7, listenId);

            Assert.Equal("signed-token", response.Ticket);
            Assert.Equal(120, response.ExpiresInSeconds);
            Assert.Equal(listenId, response.ListenId);
            Assert.Equal("https://stream.musify.test/media/Tracks/ProcessedAudios/folder-42/audio.m4a", response.ManifestUrl);
        }

        [Fact]
        public void Issue_PassesTheProcessedAudioKeyPrefixToTheTicketService()
        {
            CreateIssuer().Issue("folder-42", userId: 1, listenId: null);

            ticketService.Received(1).IssueTicket(1, "Tracks/ProcessedAudios/folder-42/", null);
        }

        [Fact]
        public void Issue_AnonymousUserWithFragmentConfigured_PassesTheByteCapToTheTicketService()
        {
            var playback = TestConfigurations.Playback(allowAnonymousListening: true, anonymousFragmentSeconds: 30);

            CreateIssuer(playback).Issue("folder-42", userId: null, listenId: null);

            ticketService.Received(1).IssueTicket(null, "Tracks/ProcessedAudios/folder-42/", 30L * playback.EstimatedAudioBytesPerSecond);
        }

        [Fact]
        public void Issue_AuthenticatedUser_IgnoresTheAnonymousFragmentCap()
        {
            var playback = TestConfigurations.Playback(allowAnonymousListening: true, anonymousFragmentSeconds: 30);

            CreateIssuer(playback).Issue("folder-42", userId: 1, listenId: null);

            ticketService.Received(1).IssueTicket(1, "Tracks/ProcessedAudios/folder-42/", null);
        }
    }
}
