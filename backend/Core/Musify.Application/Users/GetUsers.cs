using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Users.Responses;
using X.PagedList.EF;

namespace Musify.Application.Users;

public record GetUsersQuery(int PageNumber, int PageSize, string? UsernameSearch, long? ViewerId = null)
    : IQuery<ErrorOr<PaginatedResponse<UserSummaryResponse>>>;

public class GetUsersQueryHandler(IDatabase database)
    : IQueryHandler<GetUsersQuery, ErrorOr<PaginatedResponse<UserSummaryResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<UserSummaryResponse>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var usersQuery = database.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.UsernameSearch))
        {
            var normalizedUsername = request.UsernameSearch.Trim().ToUpperInvariant();
            usersQuery = usersQuery.Where(u => u.NormalizedName.Contains(normalizedUsername));
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var viewerId = request.ViewerId;
        var pagedUsers = await usersQuery
            .OrderBy(u => u.Name)
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.Name,
                u.FirstName,
                u.SecondName,
                u.ProfilePictureUrl,
                viewerId != null && database.UserFollows.Any(f => f.FollowerId == viewerId && f.FollowedId == u.Id)))
            .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

        return PaginatedResponse<UserSummaryResponse>.FromPagedList(pagedUsers);
    }
}
