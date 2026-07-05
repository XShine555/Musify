using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Handlers
{
    public class GetTrackStreamQueryHandler(
        IDatabase database,
        IStreamTicketService ticketService,
        TrackConfiguration trackConfiguration,
        StreamGatewayConfiguration streamGatewayConfiguration,
        ILogger<GetTrackStreamQueryHandler> logger)
        : IQueryHandler<GetTrackStreamQuery, Result<TrackStreamResponse>>
    {
        public async ValueTask<Result<TrackStreamResponse>> Handle(GetTrackStreamQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Where(t => t.Id == request.TrackId)
                .Select(t => new { t.Id, t.AudioTranscodeProcessingStatus, t.AudioFolderName })
                .SingleOrDefaultAsync(cancellationToken);

            if (track is null)
                return Result<TrackStreamResponse>.NotFound();

            if (string.IsNullOrWhiteSpace(track.AudioFolderName)
                || track.AudioTranscodeProcessingStatus != ProcessingStatus.Completed)
            {
                logger.LogInformation("Stream requested for track {TrackId} but audio is not ready", request.TrackId);
                return Result<TrackStreamResponse>.Conflict("Track audio is not available for streaming yet.");
            }

            var folderPath = trackConfiguration.Routes.BuildProcessedAudioPath(track.AudioFolderName);
            var keyPrefix = $"{folderPath}/";

            var ticket = ticketService.IssueTicket(request.UserId, keyPrefix);

            var manifestUrl = string.Join('/',
                streamGatewayConfiguration.PublicBaseUrl.TrimEnd('/'),
                "media",
                folderPath,
                streamGatewayConfiguration.AudioFileName);

            logger.LogInformation("Issued stream ticket for track {TrackId} to user {UserId}", request.TrackId, request.UserId);

            return Result.Success(new TrackStreamResponse(manifestUrl, ticket.Token, ticket.ExpiresInSeconds));
        }
    }
}
