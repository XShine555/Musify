using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Services
{
    /// <summary>Builds the stream URL and signed ticket for a processed track. It never touches the database.</summary>
    public sealed class TrackStreamIssuer(
        IStreamTicketService ticketService,
        TrackConfiguration trackConfiguration,
        StreamGatewayConfiguration streamGatewayConfiguration,
        PlaybackConfiguration playbackConfiguration)
    {
        public TrackStreamResponse Issue(string audioFolderName, long? userId, Guid? listenId)
        {
            var folderPath = trackConfiguration.Routes.BuildProcessedAudioPath(audioFolderName);
            var ticket = ticketService.IssueTicket(userId, $"{folderPath}/", AnonymousFragmentBytes(userId));

            var manifestUrl = string.Join('/',
                streamGatewayConfiguration.PublicBaseUrl.TrimEnd('/'),
                "media",
                folderPath,
                streamGatewayConfiguration.AudioFileName);

            return new TrackStreamResponse(manifestUrl, ticket.Token, ticket.ExpiresInSeconds, listenId);
        }

        private long? AnonymousFragmentBytes(long? userId)
        {
            if (userId != null || playbackConfiguration.AnonymousFragmentSeconds <= 0)
                return null;

            return (long)playbackConfiguration.AnonymousFragmentSeconds * playbackConfiguration.EstimatedAudioBytesPerSecond;
        }
    }
}
