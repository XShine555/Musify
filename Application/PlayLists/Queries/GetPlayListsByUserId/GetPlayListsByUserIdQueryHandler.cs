using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.PlayLists.Contracts;
using X.PagedList.EF;

namespace Musify.Application.PlayLists.Queries.GetPlayListsByUserId
{
    public class GetPlayListsByUserIdQueryHandler(IDatabase database)
        : IRequestHandler<GetPlayListsByUserIdQuery, Task<Result<PaginatedPlayListResponse>> >
    {
        public async Task<Result<PaginatedPlayListResponse>> Handle(GetPlayListsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var normalizedName = request.Name?.Trim().ToUpperInvariant() ?? string.Empty;

            var playListsQuery = database.PlayLists
                .AsNoTracking()
                .Where(p => p.UserId == request.UserId);

            if (!string.IsNullOrWhiteSpace(normalizedName))
                playListsQuery = playListsQuery.Where(p => p.NormalizedName.Contains(normalizedName));

            var totalCount = await playListsQuery.CountAsync(cancellationToken);

            var pagedPlayLists = await playListsQuery
                .OrderBy(p => p.CreatedDate)
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