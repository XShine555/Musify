using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Tracks.Responses;
using X.PagedList;
using X.PagedList.EF;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks
{
    public record GetTracksQuery(string? Name, int PageNumber, int PageSize)
        : IQuery<ErrorOr<TracksSearchResponse>>;

    public class GetTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksQuery, ErrorOr<TracksSearchResponse>>
    {
        public async ValueTask<ErrorOr<TracksSearchResponse>> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .Include(ut => ut.Track.Owner)
                .Include(ut => ut.Track.Tags)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Name))
            {
                var normalizedName = request.Name.Trim().ToUpperInvariant();
                tracksQuery = tracksQuery.Where(t => t.Track.NormalizedTitle.Contains(normalizedName));
            }

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedEntities = await tracksQuery
                .OrderByDescending(t => t.Track.CreatedAt)
                .ThenBy(t => t.Id)
                .Select(t => new { t.Track, ListensCount = t.Track.ListeningHistories.Count(l => l.IsCounted) })
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            var items = pagedEntities
                .Select(x => TrackSearchItemResponse.FromTrack(TrackApplicationResponse.FromEntity(x.Track, x.ListensCount)))
                .ToList();

            return new TracksSearchResponse(
                items,
                pagedEntities.PageNumber,
                pagedEntities.PageSize,
                pagedEntities.PageCount,
                pagedEntities.TotalItemCount,
                pagedEntities.HasPreviousPage,
                pagedEntities.HasNextPage);
        }
    }
}
