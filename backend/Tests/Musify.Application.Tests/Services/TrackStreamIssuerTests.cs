using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Services;

public sealed class TrackStreamIssuerTests : HandlerTestBase
{
    private readonly IStreamTicketService ticketService = Substitute.For<IStreamTicketService>();

    private TrackStreamIssuer CreateIssuer(PlaybackConfiguration? playback = null) => new(
        Database,
        ticketService,
        TestConfigurations.Track(),
        TestConfigurations.StreamGateway("https://stream.musify.test/"),
        playback ?? TestConfigurations.Playback());

    [Fact]
    public async Task IssueAsync_BuildsManifestUrlAndRecordsListeningHistory()
    {
        ticketService.IssueTicket(7, Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("signed-token", 120));
        var owner = TestEntities.User(7);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        var response = await CreateIssuer().IssueAsync(track.Id, "folder-42", userId: 7, TestContext.Current.CancellationToken);

        Assert.Equal("signed-token", response.Ticket);
        Assert.Equal(120, response.ExpiresInSeconds);
        Assert.Equal(
            "https://stream.musify.test/media/Tracks/ProcessedAudios/folder-42/audio.m4a",
            response.ManifestUrl);

        var history = Assert.Single(await Database.ListeningHistories.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(7, history.UserId);
        Assert.Equal(track.Id, history.TrackId);
    }

    [Fact]
    public async Task IssueAsync_PassesTheProcessedAudioKeyPrefixToTheTicketService()
    {
        ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("token", 60));
        var owner = TestEntities.User(1);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        await CreateIssuer().IssueAsync(track.Id, "folder-42", userId: 1, TestContext.Current.CancellationToken);

        ticketService.Received(1).IssueTicket(1, "Tracks/ProcessedAudios/folder-42/", null);
    }

    [Fact]
    public async Task IssueAsync_AnonymousUser_SkipsListeningHistoryAndNeverThrows()
    {
        ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("token", 60));
        var owner = TestEntities.User(1);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        var response = await CreateIssuer(TestConfigurations.Playback(allowAnonymousListening: true))
            .IssueAsync(track.Id, "folder-42", userId: null, TestContext.Current.CancellationToken);

        Assert.Equal("token", response.Ticket);
        Assert.Empty(await Database.ListeningHistories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IssueAsync_AnonymousUserWithFragmentConfigured_PassesTheByteCapToTheTicketService()
    {
        ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("token", 60));
        var owner = TestEntities.User(1);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        var playback = TestConfigurations.Playback(allowAnonymousListening: true, anonymousFragmentSeconds: 30);
        await CreateIssuer(playback).IssueAsync(track.Id, "folder-42", userId: null, TestContext.Current.CancellationToken);

        ticketService.Received(1).IssueTicket(null, "Tracks/ProcessedAudios/folder-42/", 30L * playback.EstimatedAudioBytesPerSecond);
    }

    [Fact]
    public async Task IssueAsync_AuthenticatedUser_IgnoresTheAnonymousFragmentCap()
    {
        ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("token", 60));
        var owner = TestEntities.User(1);
        var track = TestEntities.Track(owner);
        await SeedAsync(owner, track);

        var playback = TestConfigurations.Playback(allowAnonymousListening: true, anonymousFragmentSeconds: 30);
        await CreateIssuer(playback).IssueAsync(track.Id, "folder-42", userId: 1, TestContext.Current.CancellationToken);

        ticketService.Received(1).IssueTicket(1, "Tracks/ProcessedAudios/folder-42/", null);
    }
}
