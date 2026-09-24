using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using X.PagedList;
using X.PagedList.EF;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks;

public record GetTracksByUserIdQuery(
    long UserId,
    string? Name,
    int PageNumber,
    int PageSize)
    : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;

public class GetTracksByUserIdQueryHandler(IDatabase database)
    : IQueryHandler<GetTracksByUserIdQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse>>> Handle(GetTracksByUserIdQuery request, CancellationToken cancellationToken)
    {
        var tracksQuery = database.UserHasTracks
            .AsNoTracking()
            .Include(ut => ut.Track.Owner)
            .Include(ut => ut.Track.Tags)
            .Where(p => p.UserId == request.UserId && p.Track.LifeCycleStatus == LifeCycleStatus.Active);

        if (!string.IsNullOrEmpty(request.Name))
        {
            var normalizedName = request.Name.Trim().ToUpperInvariant();
            tracksQuery = tracksQuery.Where(t => t.Track.NormalizedTitle.Contains(normalizedName));
        }

        var totalCount = await tracksQuery.CountAsync(cancellationToken);

        var pagedEntities = await tracksQuery
            .OrderBy(t => t.Track.CreatedAt)
            .Select(t => new { t.Track, ListensCount = t.Track.ListeningHistories.Count(l => l.IsCounted) })
            .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

        var pagedTracks = new StaticPagedList<TrackApplicationResponse>(
            pagedEntities.Select(x => TrackApplicationResponse.FromEntity(x.Track, x.ListensCount)),
            pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

        return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
    }
}
