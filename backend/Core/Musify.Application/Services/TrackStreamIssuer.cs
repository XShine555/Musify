using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Services;

public sealed class TrackStreamIssuer(
    IDatabase database,
    IStreamTicketService ticketService,
    TrackConfiguration trackConfiguration,
    StreamGatewayConfiguration streamGatewayConfiguration)
{
    public async Task<TrackStreamResponse> IssueAsync(
        Guid trackId,
        string audioFolderName,
        long userId,
        CancellationToken cancellationToken)
    {
        var folderPath = trackConfiguration.Routes.BuildProcessedAudioPath(audioFolderName);
        var keyPrefix = $"{folderPath}/";

        var ticket = ticketService.IssueTicket(userId, keyPrefix);

        var manifestUrl = string.Join('/',
            streamGatewayConfiguration.PublicBaseUrl.TrimEnd('/'),
            "media",
            folderPath,
            streamGatewayConfiguration.AudioFileName);

        var newListeningHistory = new ListeningHistory
        {
            UserId = userId,
            TrackId = trackId,
        };
        await database.ListeningHistories.AddAsync(newListeningHistory, cancellationToken);
        await database.SaveChangesAsync(cancellationToken);

        return new TrackStreamResponse(manifestUrl, ticket.Token, ticket.ExpiresInSeconds);
    }
}
