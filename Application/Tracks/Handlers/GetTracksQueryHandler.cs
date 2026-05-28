using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Pagination;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;
using X.PagedList.EF;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks.Handlers
{
    public class GetTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksQuery, Result<PaginatedResponse<TrackApplicationResponse>> >
    {
        public async ValueTask<Result<PaginatedResponse<TrackApplicationResponse> >> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .Include(ut => ut.Track)
                .Select(t => TrackApplicationResponse.FromEntity(t.Track));

            var totalCount = await tracksQuery.CountAsync(cancellationToken);
            var pagedTracks = await tracksQuery.ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks));
        }
    }
}