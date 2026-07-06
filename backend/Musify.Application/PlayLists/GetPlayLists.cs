using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Shared;
using Musify.Application.PlayLists.Responses;
using X.PagedList.EF;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists
{
    public record GetPlayListsQuery(int PageNumber, int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<PlayListApplicationResponse>> >;

    public class GetPlayListsQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsQuery, ErrorOr<PaginatedResponse<PlayListApplicationResponse> >>
    {
        public async ValueTask<ErrorOr<PaginatedResponse<PlayListApplicationResponse> >> Handle(GetPlayListsQuery request, CancellationToken cancellationToken)
        {
            var totalCount = await database.PlayLists.CountAsync(cancellationToken);

            var pagedPlayLists = await database.PlayLists
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => PlayListApplicationResponse.FromEntity(p))
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return PaginatedResponse<PlayListApplicationResponse>.FromPagedList(pagedPlayLists);
        }
    }
}
