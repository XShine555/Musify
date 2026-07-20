using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Shared;
using Musify.Application.PlayLists.Responses;
using X.PagedList;
using X.PagedList.EF;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists
{
    public record GetPlayListsByUserIdQuery(
        long UserId,
        string? Name,
        int PageNumber,
        int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<PlayListApplicationResponse>> >;

    public class GetPlayListsByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsByUserIdQuery, ErrorOr<PaginatedResponse<PlayListApplicationResponse> > >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<PlayListApplicationResponse> >> Handle(GetPlayListsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var playListsQuery = database.PlayLists
                .AsNoTracking()
                .Where(p => p.UserId == request.UserId);

            if (!string.IsNullOrEmpty(request.Name))
            {
                var normalizedName = request.Name.Trim().ToUpperInvariant();
                playListsQuery = playListsQuery.Where(p => p.NormalizedName.Contains(normalizedName));
            }

            var totalCount = await playListsQuery.CountAsync(cancellationToken);

            var pagedEntities = await playListsQuery
                .OrderBy(p => p.CreatedAt)
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
}
