using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;
using X.PagedList.EF;

namespace Musify.Application.Tracks.Handler
{
    public class GetTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksQuery, Result<PaginatedResponse<TrackResponse>> >
    {
        public async ValueTask<Result<PaginatedResponse<TrackResponse> >> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .Include(ut => ut.Track)
                .Select(t => TrackResponse.FromEntity(t.Track));

            var totalCount = await tracksQuery.CountAsync(cancellationToken);
            var pagedTracks = await tracksQuery.ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<TrackResponse>.FromPagedList(pagedTracks));
        }
    }
}