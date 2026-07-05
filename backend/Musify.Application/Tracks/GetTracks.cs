using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Pagination;
using Musify.Application.Tracks.Responses;
using X.PagedList.EF;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks
{
    public record GetTracksQuery(int PageNumber = 1, int PageSize = 10)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse> >>;

    public class GetTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>> >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse> >> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .Include(ut => ut.Track)
                .Select(t => TrackApplicationResponse.FromEntity(t.Track));

            var totalCount = await tracksQuery.CountAsync(cancellationToken);
            var pagedTracks = await tracksQuery.ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
        }
    }
}
