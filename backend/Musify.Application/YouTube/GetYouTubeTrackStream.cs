using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.YouTube.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.YouTube
{
    public record GetYouTubeTrackStreamQuery(string VideoId, long UserId)
        : IQuery<ErrorOr<YouTubeStreamResponse>>;

    public class GetYouTubeTrackStreamQueryHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        TrackStreamIssuer streamIssuer)
        : IQueryHandler<GetYouTubeTrackStreamQuery, ErrorOr<YouTubeStreamResponse>>
    {
        public async ValueTask<ErrorOr<YouTubeStreamResponse>> Handle(GetYouTubeTrackStreamQuery request, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.AsNoTracking()
                .Where(t => t.Source == TrackSource.YouTube && t.ExternalId == request.VideoId)
                .Select(t => new { t.Id, t.AudioTranscodeProcessingStatus, t.AudioFolderName } )
                .SingleOrDefaultAsync(cancellationToken);

            if (track != null
                && !string.IsNullOrWhiteSpace(track.AudioFolderName)
                && track.AudioTranscodeProcessingStatus == ProcessingStatus.Completed)
            {
                var serverStream = await streamIssuer.IssueAsync(track.Id, track.AudioFolderName, request.UserId, cancellationToken);
                return new YouTubeStreamResponse(
                    YouTubeStreamMode.Server,
                    serverStream.ManifestUrl,
                    serverStream.Ticket,
                    serverStream.ExpiresInSeconds,
                    track.Id);
            }

            var streamInfo = await youTubeMusicService.GetAudioStreamAsync(request.VideoId, cancellationToken);
            if (streamInfo.IsError)
                return streamInfo.Errors;

            return new YouTubeStreamResponse(
                YouTubeStreamMode.YouTube,
                streamInfo.Value.Url,
                string.Empty,
                streamInfo.Value.ExpiresInSeconds,
                Guid.Empty);
        }
    }
}
