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

            var totalCount = await database.PlayLists.CountAsync(cancellationToken);

            var pagedPlayLists = await database.UserHasTracks
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .Include(ut => ut.Track)
                .Select(t => TrackResponse.FromEntity(t.Track))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            var response = new PaginatedResponse<TrackResponse>(
                pagedPlayLists.ToArray(),
                pagedPlayLists.PageNumber,
                pagedPlayLists.PageSize,
                pagedPlayLists.PageCount,
                pagedPlayLists.TotalItemCount,
                pagedPlayLists.HasNextPage,
                pagedPlayLists.HasPreviousPage);

            return Result.Success(response);
        }
    }
}