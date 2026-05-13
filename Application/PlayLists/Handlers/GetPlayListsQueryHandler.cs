using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Abstractions.Application;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.PlayLists.Responses;
using Musify.Application.PlayLists.Queries;
using X.PagedList.EF;
using Mediator;

namespace Musify.Application.PlayLists.Handlers
{
    public class GetPlayListsQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsQuery, Result<PaginatedResponse<PlayListApplicationResponse> >>
    {
        public async ValueTask<Result<PaginatedResponse<PlayListApplicationResponse> >> Handle(GetPlayListsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var totalCount = await database.PlayLists.CountAsync(cancellationToken);

            var pagedPlayLists = await database.PlayLists
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => PlayListApplicationResponse.FromEntity(p))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<PlayListApplicationResponse>.FromPagedList(pagedPlayLists));
        }
    }
}