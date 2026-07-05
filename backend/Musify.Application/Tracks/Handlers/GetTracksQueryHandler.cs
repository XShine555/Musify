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
            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .Include(ut => ut.Track)
                .Select(t => TrackApplicationResponse.FromEntity(t.Track));

            var totalCount = await tracksQuery.CountAsync(cancellationToken);
            var pagedTracks = await tracksQuery.ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks));
        }
    }
}