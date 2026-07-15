using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.PlayLists
{
    public record GetPlayListTracksQuery(
        Guid PlayListId,
        int PageNumber,
        int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;

    public class GetPlayListTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>> >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse> >> Handle(GetPlayListTracksQuery request, CancellationToken cancellationToken)
        {
            var playListExists = await database.PlayLists
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.PlayListId, cancellationToken);
            if (!playListExists)
                return Error.NotFound();

            var tracksQuery = database.PlayListHasTracks
                .AsNoTracking()
                .Include(plt => plt.Track)
                .Where(plt => plt.PlayListId == request.PlayListId);

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedEntities = await tracksQuery
                .OrderBy(plt => plt.Position)
                .Select(plt => new { plt.Track, ListensCount = plt.Track.ListeningHistories.Count })
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            var pagedTracks = new StaticPagedList<TrackApplicationResponse>(
                pagedEntities.Select(x => TrackApplicationResponse.FromEntity(x.Track, x.ListensCount)),
                pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

            return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
        }
    }
}
