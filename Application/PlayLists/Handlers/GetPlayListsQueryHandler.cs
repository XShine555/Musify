using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.PlayLists.Responses;
using Musify.Application.PlayLists.Queries;
using X.PagedList.EF;
using Mediator;

namespace Musify.Application.PlayLists.Handlers
{
    public class GetPlayListsQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsQuery, Result<PaginatedResponse<PlayListResponse> >>
    {
        public async ValueTask<Result<PaginatedResponse<PlayListResponse> >> Handle(GetPlayListsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var totalCount = await database.PlayLists.CountAsync(cancellationToken);

            var pagedPlayLists = await database.PlayLists
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => PlayListResponse.FromEntity(p))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            var response = new PaginatedResponse<PlayListResponse>(
                pagedPlayLists.ToArray(),
                pagedPlayLists.PageNumber,
                pagedPlayLists.PageSize,
                pagedPlayLists.PageCount,
                pagedPlayLists.TotalItemCount,
                pagedPlayLists.HasNextPage,
                pagedPlayLists.HasPreviousPage);

            return Result.Success(response);
        }
    }
}