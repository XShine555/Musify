using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Shared;
using Musify.Application.PlayLists.Responses;
using X.PagedList.EF;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists
{
    public record GetPlayListsByUserIdQuery(
        long UserId,
        string? Name,
        int PageNumber = 1,
        int PageSize = 10)
        : IQuery<ErrorOr<PaginatedResponse<PlayListApplicationResponse>> >;

    public class GetPlayListsByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsByUserIdQuery, ErrorOr<PaginatedResponse<PlayListApplicationResponse> > >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<PlayListApplicationResponse> >> Handle(GetPlayListsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var playListsQuery = database.PlayLists
                .AsNoTracking()
                .Where(p => p.UserId == request.UserId);

            if (request.Name is not null)
            {
                var normalizedName = request.Name.Trim().ToUpperInvariant();
                playListsQuery = playListsQuery.Where(p => p.NormalizedName.Contains(normalizedName));
            }

            var totalCount = await playListsQuery.CountAsync(cancellationToken);

            var pagedPlayLists = await playListsQuery
                .OrderBy(p => p.CreatedAt)
                .Select(p => PlayListApplicationResponse.FromEntity(p))
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return PaginatedResponse<PlayListApplicationResponse>.FromPagedList(pagedPlayLists);
        }
    }
}
