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
    public class GetPlayListsByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsByUserIdQuery, Result<PaginatedResponse<PlayListResponse> > >
    {
        public async ValueTask<Result<PaginatedResponse<PlayListResponse> >> Handle(GetPlayListsByUserIdQuery request, CancellationToken cancellationToken)
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

            return Result.Success(PaginatedResponse<PlayListResponse>.FromPagedList(pagedPlayLists));
        }
    }
}