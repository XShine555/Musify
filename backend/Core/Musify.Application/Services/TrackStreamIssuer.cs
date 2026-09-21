using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Services;

public sealed class TrackStreamIssuer(
    IDatabase database,
    IStreamTicketService ticketService,
    TrackConfiguration trackConfiguration,
    StreamGatewayConfiguration streamGatewayConfiguration,
    PlaybackConfiguration playbackConfiguration)
{
    public async Task<TrackStreamResponse> IssueAsync(
        Guid trackId,
        string audioFolderName,
        long? userId,
        CancellationToken cancellationToken)
    {
        var folderPath = trackConfiguration.Routes.BuildProcessedAudioPath(audioFolderName);
        var keyPrefix = $"{folderPath}/";

        var maxBytes = AnonymousFragmentBytes(userId);
        var ticket = ticketService.IssueTicket(userId, keyPrefix, maxBytes);

        var manifestUrl = string.Join('/',
            streamGatewayConfiguration.PublicBaseUrl.TrimEnd('/'),
            "media",
            folderPath,
            streamGatewayConfiguration.AudioFileName);

        if (userId != null)
        {
            var newListeningHistory = new ListeningHistory
            {
                UserId = userId.Value,
                TrackId = trackId,
            };
            await database.ListeningHistories.AddAsync(newListeningHistory, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
        }

        return new TrackStreamResponse(manifestUrl, ticket.Token, ticket.ExpiresInSeconds);
    }

    private long? AnonymousFragmentBytes(long? userId)
    {
        if (userId != null || playbackConfiguration.AnonymousFragmentSeconds <= 0)
            return null;

        return (long)playbackConfiguration.AnonymousFragmentSeconds * playbackConfiguration.EstimatedAudioBytesPerSecond;
    }
}
