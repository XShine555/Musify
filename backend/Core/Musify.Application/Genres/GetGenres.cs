using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Genres.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Genres;

public record GetGenresQuery : IQuery<IReadOnlyList<GenreResponse>>;

public class GetGenresQueryHandler(IDatabase database)
    : IQueryHandler<GetGenresQuery, IReadOnlyList<GenreResponse>>
{
    public async ValueTask<IReadOnlyList<GenreResponse>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        var rows = await database.TrackTags
            .AsNoTracking()
            .Where(tag => tag.Track.LifeCycleStatus == LifeCycleStatus.Active)
            .GroupBy(tag => tag.Tag)
            .Select(group => new { Genre = group.Key, TrackCount = group.Select(tag => tag.TrackId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.TrackCount)
            .ThenBy(row => row.Genre)
            .Select(row => new GenreResponse(row.Genre, row.TrackCount))
            .ToList();
    }
}
