using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Queries;
using Musify.Application.Users.Responses;
using X.PagedList.EF;
using X.PagedList.Extensions;

namespace Musify.Application.Users.Handlers
{
    public class GetUsersQueryHandler(IDatabase database)
        : IQueryHandler<GetUsersQuery, Result<PaginatedResponse<UserResponse> > >
    {
        public async ValueTask<Result<PaginatedResponse<UserResponse> >> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var first = (pageNumber - 1) * pageSize;

            var user = database.Users
                .AsNoTracking()
                .AsQueryable();
            if (!string.IsNullOrEmpty(request.UsernameSearch))
            {
                var normalizedUsername = request.UsernameSearch.Trim().ToUpperInvariant();
                user = user.Where(u => u.NormalizedName.Contains(normalizedUsername));
            }

            var totalCount = await user.CountAsync(cancellationToken);
            var pagedUser = await user
                .Select(u => UserResponse.FromEntity(u))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<UserResponse>.FromPagedList(pagedUser));
        }
    }
}