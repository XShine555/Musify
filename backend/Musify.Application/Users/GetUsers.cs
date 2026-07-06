using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Shared;
using Musify.Application.Users.Responses;
using X.PagedList.EF;
using Musify.Application.Contracts;

namespace Musify.Application.Users
{
    public record GetUsersQuery(int PageNumber, int PageSize, string? UsernameSearch = null)
        : IQuery<ErrorOr<PaginatedResponse<UserApplicationResponse> >>;

    public class GetUsersQueryHandler(IDatabase database)
        : IQueryHandler<GetUsersQuery, ErrorOr<PaginatedResponse<UserApplicationResponse> > >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<UserApplicationResponse> >> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
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
                .Select(u => UserApplicationResponse.FromEntity(u))
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            return PaginatedResponse<UserApplicationResponse>.FromPagedList(pagedUser);
        }
    }
}
