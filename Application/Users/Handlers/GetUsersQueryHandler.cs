using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Abstractions.Application;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Users.Queries;
using Musify.Application.Users.Responses;
using X.PagedList.EF;

namespace Musify.Application.Users.Handlers
{
    public class GetUsersQueryHandler(IDatabase database)
        : IQueryHandler<GetUsersQuery, Result<PaginatedResponse<UserApplicationResponse> > >
    {
        public async ValueTask<Result<PaginatedResponse<UserApplicationResponse> >> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var user = database.Users
                .AsNoTracking()
                .AsQueryable();

            if (request.UsernameSearch is not null)
            {
                var normalizedUsername = request.UsernameSearch.Trim().ToUpperInvariant();
                user = user.Where(u => u.NormalizedName.Contains(normalizedUsername));
            }

            var totalCount = await user.CountAsync(cancellationToken);
            var pagedUser = await user
                .Select(u => UserApplicationResponse.FromEntity(u))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<UserApplicationResponse>.FromPagedList(pagedUser));
        }
    }
}