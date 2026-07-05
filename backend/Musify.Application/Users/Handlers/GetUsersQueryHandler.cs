using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Pagination;
using Musify.Application.Users.Queries;
using Musify.Application.Users.Responses;
using X.PagedList.EF;
using Musify.Application.Contracts;

namespace Musify.Application.Users.Handlers
{
    public class GetUsersQueryHandler(IDatabase database)
        : IQueryHandler<GetUsersQuery, ErrorOr<PaginatedResponse<UserApplicationResponse> > >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<UserApplicationResponse> >> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
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
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return PaginatedResponse<UserApplicationResponse>.FromPagedList(pagedUser);
        }
    }
}