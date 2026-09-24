using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks
{
    public record GetListeningHistoryQuery(long UserId) : IQuery<IReadOnlyList<TrackApplicationResponse>>;

    public class GetListeningHistoryQueryHandler(IDatabase database)
        : IQueryHandler<GetListeningHistoryQuery, IReadOnlyList<TrackApplicationResponse>>
    {
        public const int ListSize = 15;

        public async ValueTask<IReadOnlyList<TrackApplicationResponse>> Handle(GetListeningHistoryQuery query, CancellationToken cancellationToken)
        {
            var recentTrackIds = await database.ListeningHistories
                .AsNoTracking()
                .Where(l => l.UserId == query.UserId && l.IsCounted)
                .GroupBy(l => l.TrackId)
                .OrderByDescending(g => g.Max(l => l.ListenedAt))
                .ThenBy(g => g.Key)
                .Select(g => g.Key)
                .Take(ListSize)
                .ToListAsync(cancellationToken);

            var tracksById = await database.Tracks
                .AsNoTracking()
                .Active()
                .Where(t => recentTrackIds.Contains(t.Id))
                .SelectResponse()
                .ToDictionaryAsync(t => t.Id, cancellationToken);

            return recentTrackIds
                .Where(id => tracksById.ContainsKey(id))
                .Select(id => tracksById[id])
                .ToList();
        }
    }
}
