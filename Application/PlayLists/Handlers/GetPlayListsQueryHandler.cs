using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Pagination;
using Musify.Application.PlayLists.Responses;
using Musify.Application.PlayLists.Queries;
using X.PagedList.EF;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists.Handlers
{
    public class GetPlayListsQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsQuery, Result<PaginatedResponse<PlayListApplicationResponse> >>
    {
        public async ValueTask<Result<PaginatedResponse<PlayListApplicationResponse> >> Handle(GetPlayListsQuery request, CancellationToken cancellationToken)
        {
            var totalCount = await database.PlayLists.CountAsync(cancellationToken);

            var pagedPlayLists = await database.PlayLists
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => PlayListApplicationResponse.FromEntity(p))
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<PlayListApplicationResponse>.FromPagedList(pagedPlayLists));
        }
    }
}