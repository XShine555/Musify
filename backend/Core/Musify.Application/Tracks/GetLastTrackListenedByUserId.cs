using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks
{
    public record GetLastTrackListenedByUserIdQuery(long UserId) : IQuery<ErrorOr<TrackApplicationResponse>>;

    public class GetLastTrackListenedByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetLastTrackListenedByUserIdQuery, ErrorOr<TrackApplicationResponse>>
    {
        public async ValueTask<ErrorOr<TrackApplicationResponse>> Handle(GetLastTrackListenedByUserIdQuery query, CancellationToken cancellationToken)
        {
            var track = await database.ListeningHistories
                .AsNoTracking()
                .Include(t => t.Track.Owner)
                .Include(t => t.Track.Tags)
                .Where(t => t.UserId == query.UserId)
                .OrderByDescending(t => t.ListenedAt)
                .Select(t => TrackApplicationResponse.FromEntity(t.Track, t.Track.ListeningHistories.Count(l => l.IsCounted)))
                .FirstOrDefaultAsync(cancellationToken);

            return track == null
                ? Error.NotFound("Track.NotFound", "No track found for the given user ID.")
                : track;
        }
    }
}