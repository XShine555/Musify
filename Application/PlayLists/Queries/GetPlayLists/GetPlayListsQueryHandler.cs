using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.PlayLists.Contracts;
using Musify.Domain.Entities;
using X.PagedList.EF;

namespace Musify.Application.PlayLists.Queries.GetPlayLists
{
    public class GetPlayListsQueryHandler(IDatabase database)
        : IRequestHandler<GetPlayListsQuery, Task<Result<PaginatedPlayListResponse>> >
    {
        public async Task<Result<PaginatedPlayListResponse>> Handle(GetPlayListsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var totalCount = await database.PlayLists.CountAsync(cancellationToken);

            var pagedPlayLists = await database.PlayLists
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => PlayListResponse.FromEntity(p))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            var response = new PaginatedPlayListResponse(
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