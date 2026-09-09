using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Services;

public sealed class TrackStreamIssuerTests : HandlerTestBase
{
    private readonly IStreamTicketService ticketService = Substitute.For<IStreamTicketService>();

    private TrackStreamIssuer CreateIssuer() => new(
        Database, ticketService, TestConfigurations.Track(), TestConfigurations.StreamGateway("https://stream.musify.test/"));

    [Fact]
    public async Task IssueAsync_BuildsManifestUrlAndRecordsListeningHistory()
    {
        ticketService.IssueTicket(7, Arg.Any<string>()).Returns(new StreamTicket("signed-token", 120));
        var owner = TestEntities.User(7);
        var track = TestEntities.LocalTrack(owner);
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
        ticketService.IssueTicket(Arg.Any<long>(), Arg.Any<string>()).Returns(new StreamTicket("token", 60));
        var owner = TestEntities.User(1);
        var track = TestEntities.LocalTrack(owner);
        await SeedAsync(owner, track);

        await CreateIssuer().IssueAsync(track.Id, "folder-42", userId: 1, TestContext.Current.CancellationToken);

        ticketService.Received(1).IssueTicket(1, "Tracks/ProcessedAudios/folder-42/");
    }
}
