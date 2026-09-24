using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.PlayLists;

public record GetPlayListTracksQuery(
    Guid PlayListId,
    int PageNumber,
    int PageSize,
    long? ViewerId = null)
    : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;

public class GetPlayListTracksQueryHandler(IDatabase database)
    : IQueryHandler<GetPlayListTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse>>> Handle(GetPlayListTracksQuery request, CancellationToken cancellationToken)
    {
        var playListExists = await database.PlayLists
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == request.PlayListId
                    && p.LifeCycleStatus == LifeCycleStatus.Active
                    && (p.Visibility == PlaylistVisibility.Public || p.UserId == request.ViewerId),
                cancellationToken);
        if (!playListExists)
            return Error.NotFound();

        var tracksQuery = database.PlayListHasTracks
            .AsNoTracking()
            .Include(plt => plt.Track.Owner)
            .Include(plt => plt.Track.Tags)
            .Where(plt => plt.PlayListId == request.PlayListId && plt.Track.LifeCycleStatus == LifeCycleStatus.Active);

        var totalCount = await tracksQuery.CountAsync(cancellationToken);

        var pagedEntities = await tracksQuery
            .OrderBy(plt => plt.Position)
            .Select(plt => new { plt.Track, ListensCount = plt.Track.ListeningHistories.Count(l => l.IsCounted) })
            .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

        var pagedTracks = new StaticPagedList<TrackApplicationResponse>(
            pagedEntities.Select(x => TrackApplicationResponse.FromEntity(x.Track, x.ListensCount)),
            pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

        return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
    }
}
