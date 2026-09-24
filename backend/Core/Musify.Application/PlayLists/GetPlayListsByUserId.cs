using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.PlayLists;

public record GetPlayListsByUserIdQuery(
    long UserId,
    string? Name,
    int PageNumber,
    int PageSize,
    long? ViewerId = null)
    : IQuery<ErrorOr<PaginatedResponse<PlayListApplicationResponse>>>;

public class GetPlayListsByUserIdQueryHandler(IDatabase database)
    : IQueryHandler<GetPlayListsByUserIdQuery, ErrorOr<PaginatedResponse<PlayListApplicationResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<PlayListApplicationResponse>>> Handle(GetPlayListsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var playListsQuery = database.PlayLists
            .AsNoTracking()
            .Where(p => p.OwnerUserId == request.UserId
                && p.LifeCycleStatus == LifeCycleStatus.Active
                && (p.Visibility == PlayListVisibility.Public || p.OwnerUserId == request.ViewerId));

        if (!string.IsNullOrEmpty(request.Name))
        {
            var normalizedName = request.Name.Trim().ToUpperInvariant();
            playListsQuery = playListsQuery.Where(p => p.NormalizedName.Contains(normalizedName));
        }

        var totalCount = await playListsQuery.CountAsync(cancellationToken);

        var pagedEntities = await playListsQuery
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
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
