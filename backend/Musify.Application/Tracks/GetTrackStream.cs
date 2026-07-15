using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record GetTrackStreamQuery(Guid TrackId, long UserId)
        : IQuery<ErrorOr<TrackStreamResponse>>;

    public class GetTrackStreamQueryHandler(
        IDatabase database,
        TrackStreamIssuer streamIssuer,
        ILogger<GetTrackStreamQueryHandler> logger)
        : IQueryHandler<GetTrackStreamQuery, ErrorOr<TrackStreamResponse>>
    {
        public async ValueTask<ErrorOr<TrackStreamResponse>> Handle(GetTrackStreamQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Where(t => t.Id == request.TrackId)
                .Select(t => new { t.Id, t.AudioTranscodeProcessingStatus, t.AudioFolderName } )
                .SingleOrDefaultAsync(cancellationToken);

            if (track == null)
                return Error.NotFound();

            if (string.IsNullOrWhiteSpace(track.AudioFolderName)
                || track.AudioTranscodeProcessingStatus != ProcessingStatus.Completed)
            {
                logger.LogInformation("Stream requested for track {TrackId} but audio is not ready", request.TrackId);
                return Error.Conflict(description: "Track audio is not available for streaming yet.");
            }

            var response = await streamIssuer.IssueAsync(track.Id, track.AudioFolderName, request.UserId, cancellationToken);

            logger.LogInformation("Issued stream ticket for track {TrackId} to user {UserId}", request.TrackId, request.UserId);

            return response;
        }
    }
}
