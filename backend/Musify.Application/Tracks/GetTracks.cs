using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using X.PagedList.EF;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks
{
    public record GetTracksQuery(string? Name = null, int PageNumber = 1, int PageSize = 10)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse> >>;

    public class GetTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>> >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse> >> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .Include(ut => ut.Track)
                .AsQueryable();

            if (request.Name is not null)
            {
                var normalizedName = request.Name.Trim().ToUpperInvariant();
                tracksQuery = tracksQuery.Where(t => t.Track.NormalizedTitle.Contains(normalizedName));
            }

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedTracks = await tracksQuery
                .OrderBy(t => t.Id)
                .Select(t => TrackApplicationResponse.FromEntity(t.Track))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
        }
    }
}
