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
    public class GetPlayListsByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListsByUserIdQuery, Result<PaginatedResponse<PlayListApplicationResponse> > >
    {
        public async ValueTask<Result<PaginatedResponse<PlayListApplicationResponse> >> Handle(GetPlayListsByUserIdQuery request, CancellationToken cancellationToken)
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
                .OrderBy(p => p.CreatedDate)
                .Select(p => PlayListApplicationResponse.FromEntity(p))
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<PlayListApplicationResponse>.FromPagedList(pagedPlayLists));
        }
    }
}