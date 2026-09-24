using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users;

public record GetUsersQuery(int PageNumber, int PageSize, string? UsernameSearch, long? ViewerId = null)
    : IQuery<PaginatedResponse<UserSummaryResponse>>;

public class GetUsersQueryHandler(IDatabase database)
    : IQueryHandler<GetUsersQuery, PaginatedResponse<UserSummaryResponse>>
{
    public async ValueTask<PaginatedResponse<UserSummaryResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var usersQuery = database.Users
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.UsernameSearch))
        {
            var normalizedUsername = TextNormalizer.Normalize(request.UsernameSearch);
            usersQuery = usersQuery.Where(user => user.NormalizedName.Contains(normalizedUsername));
        }

        var viewerId = request.ViewerId;

        return await usersQuery
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Id)
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.Name,
                user.FirstName,
                user.SecondName,
                user.ProfilePictureUrl,
                viewerId != null && database.UserFollows.Any(f => f.FollowerId == viewerId && f.FollowedId == user.Id)))
            .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
    }
}
