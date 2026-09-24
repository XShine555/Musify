using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks;

public record GetLastTrackListenedByUserIdQuery(long UserId) : IQuery<TrackApplicationResponse?>;

public class GetLastTrackListenedByUserIdQueryHandler(IDatabase database)
    : IQueryHandler<GetLastTrackListenedByUserIdQuery, TrackApplicationResponse?>
{
    public async ValueTask<TrackApplicationResponse?> Handle(GetLastTrackListenedByUserIdQuery query, CancellationToken cancellationToken)
    {
        return await database.ListeningHistories
            .AsNoTracking()
            .Include(t => t.Track.Owner)
            .Include(t => t.Track.Tags)
            .Where(t => t.UserId == query.UserId && t.IsCounted && t.Track.LifeCycleStatus == LifeCycleStatus.Active)
            .OrderByDescending(t => t.ListenedAt)
            .ThenBy(t => t.Id)
            .Select(t => TrackApplicationResponse.FromEntity(t.Track, t.Track.ListeningHistories.Count(l => l.IsCounted)))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
