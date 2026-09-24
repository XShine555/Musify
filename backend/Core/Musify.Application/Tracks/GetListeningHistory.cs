using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks;

public record GetListeningHistoryQuery(long UserId) : IQuery<IEnumerable<TrackApplicationResponse>>;

public class GetListeningHistoryQueryHandler(
    IDatabase database)
    : IQueryHandler<GetListeningHistoryQuery, IEnumerable<TrackApplicationResponse>>
{
    public const int ListSize = 15;

    public async ValueTask<IEnumerable<TrackApplicationResponse>> Handle(GetListeningHistoryQuery query, CancellationToken cancellationToken)
    {
        var recentTrackIds = await database.ListeningHistories
            .AsNoTracking()
            .Where(l => l.UserId == query.UserId && l.IsCounted)
            .GroupBy(l => l.TrackId)
            .OrderByDescending(g => g.Max(l => l.ListenedAt))
            .Select(g => g.Key)
            .Take(ListSize)
            .ToListAsync(cancellationToken);

        var tracksById = await database.Tracks
            .AsNoTracking()
            .Include(t => t.Owner)
            .Include(t => t.Tags)
            .Where(t => recentTrackIds.Contains(t.Id))
            .Select(t => new { t.Id, Track = t, ListensCount = t.ListeningHistories.Count(l => l.IsCounted) })
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return recentTrackIds
            .Select(id => tracksById[id])
            .Select(t => TrackApplicationResponse.FromEntity(t.Track, t.ListensCount));
    }
}
