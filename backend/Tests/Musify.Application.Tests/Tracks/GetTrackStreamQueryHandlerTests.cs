using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class GetTrackStreamQueryHandlerTests : HandlerTestBase
{
    private readonly IStreamTicketService ticketService = Substitute.For<IStreamTicketService>();

    private GetTrackStreamQueryHandler CreateHandler()
    {
        ticketService.IssueTicket(Arg.Any<long>(), Arg.Any<string>()).Returns(new StreamTicket("ticket-token", 60));

        var issuer = new TrackStreamIssuer(
            Database,
            ticketService,
            TestConfigurations.Track(),
            TestConfigurations.StreamGateway("https://stream.musify.test"));

        return new GetTrackStreamQueryHandler(Database, issuer, NoOpLogger<GetTrackStreamQueryHandler>());
    }

    [Fact]
    public async Task Handle_ProcessedTrack_IssuesTicketAndRecordsListen()
    {
        var owner = TestEntities.User();
        var track = TestEntities.LocalTrack(owner);
        await SeedAsync(owner, track);

        var result = await CreateHandler().Handle(new GetTrackStreamQuery(track.Id, owner.Id), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("ticket-token", result.Value.Ticket);
        Assert.Equal(60, result.Value.ExpiresInSeconds);
        Assert.Contains("audio-folder", result.Value.ManifestUrl);

        var history = Assert.Single(await Database.ListeningHistories.ToListAsync());
        Assert.Equal(owner.Id, history.UserId);
        Assert.Equal(track.Id, history.TrackId);
    }

    [Fact]
    public async Task Handle_TrackMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new GetTrackStreamQuery(Guid.NewGuid(), 1), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_AudioNotProcessedYet_ReturnsConflict()
    {
        var owner = TestEntities.User();
        var track = TestEntities.LocalTrack(owner, audio: TestEntities.PendingAudio());
        await SeedAsync(owner, track);

        var result = await CreateHandler().Handle(new GetTrackStreamQuery(track.Id, owner.Id), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }
}
