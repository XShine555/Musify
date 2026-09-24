using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.PlayLists;

public record GetPlayListsQuery(int PageNumber, int PageSize)
    : IQuery<ErrorOr<PaginatedResponse<PlayListApplicationResponse>>>;

public class GetPlayListsQueryHandler(IDatabase database)
    : IQueryHandler<GetPlayListsQuery, ErrorOr<PaginatedResponse<PlayListApplicationResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<PlayListApplicationResponse>>> Handle(GetPlayListsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await database.PlayLists.CountAsync(cancellationToken);

        var pagedEntities = await database.PlayLists
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                PlayList = p,
                CoverTrackIds = p.PlayListTracks
                    .OrderBy(plt => plt.Position)
                    .Take(PlayListApplicationResponse.CoverTrackCount)
                    .Select(plt => plt.TrackId)
                    .ToList()
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

        var pagedPlayLists = new StaticPagedList<PlayListApplicationResponse>(
            pagedEntities.Select(entry => PlayListApplicationResponse.FromEntity(entry.PlayList, entry.CoverTrackIds)),
            pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

        return PaginatedResponse<PlayListApplicationResponse>.FromPagedList(pagedPlayLists);
    }
}
