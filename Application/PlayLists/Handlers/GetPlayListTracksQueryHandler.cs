using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Pagination;
using Musify.Application.PlayLists.Queries;
using Musify.Application.Tracks.Responses;
using X.PagedList.EF;

namespace Musify.Application.PlayLists.Handlers
{
    public class GetPlayListTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListTracksQuery, Result<PaginatedResponse<TrackApplicationResponse>> >
    {
        public async ValueTask<Result<PaginatedResponse<TrackApplicationResponse> >> Handle(GetPlayListTracksQuery request, CancellationToken cancellationToken)
        {
            var playListExists = await database.PlayLists
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.PlayListId, cancellationToken);
            if (!playListExists)
                return Result<PaginatedResponse<TrackApplicationResponse>>.NotFound();

            var tracksQuery = database.PlayListHasTracks
                .AsNoTracking()
                .Include(plt => plt.Track)
                .Where(plt => plt.PlayListId == request.PlayListId);

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedTracks = await tracksQuery
                .OrderBy(plt => plt.Position)
                .Select(plt => TrackApplicationResponse.FromEntity(plt.Track))
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks));
        }
    }
}
