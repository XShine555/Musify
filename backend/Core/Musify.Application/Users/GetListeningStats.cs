using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users;

public record GetListeningStatsQuery(long UserId) : IQuery<ListeningStatsResponse>;

public class GetListeningStatsQueryHandler(IDatabase database)
    : IQueryHandler<GetListeningStatsQuery, ListeningStatsResponse>
{
    public async ValueTask<ListeningStatsResponse> Handle(GetListeningStatsQuery request, CancellationToken cancellationToken)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var thisWeekHistory = database.ListeningHistories
            .AsNoTracking()
            .Where(l => l.UserId == request.UserId && l.ListenedAt >= weekAgo);

        var tracksThisWeek = await thisWeekHistory
            .Where(l => l.IsCounted)
            .Select(l => l.TrackId)
            .Distinct()
            .CountAsync(cancellationToken);

        var secondsThisWeek = await thisWeekHistory
            .Join(database.Tracks.AsNoTracking(), l => l.TrackId, t => t.Id, (l, t) => l.PlayedSeconds ?? (l.IsCounted ? t.DurationSeconds : 0))
            .SumAsync(cancellationToken);

        var listenedDates = await database.ListeningHistories
            .AsNoTracking()
            .Where(l => l.UserId == request.UserId && l.IsCounted)
            .Select(l => l.ListenedAt.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        var listenedDateSet = listenedDates.ToHashSet();
        var streakDays = 0;
        var day = DateTime.UtcNow.Date;
        if (!listenedDateSet.Contains(day))
            day = day.AddDays(-1);

        while (listenedDateSet.Contains(day))
        {
            streakDays++;
            day = day.AddDays(-1);
        }

        return new ListeningStatsResponse(tracksThisWeek, (int)Math.Round(secondsThisWeek), streakDays);
    }
}
