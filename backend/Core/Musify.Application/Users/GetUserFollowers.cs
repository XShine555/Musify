using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users;

public record GetUserFollowersQuery(long? ViewerId, long UserId, int PageNumber, int PageSize)
    : IQuery<ErrorOr<PaginatedResponse<UserSummaryResponse>>>;

public class GetUserFollowersQueryHandler(IDatabase database)
    : IQueryHandler<GetUserFollowersQuery, ErrorOr<PaginatedResponse<UserSummaryResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<UserSummaryResponse>>> Handle(GetUserFollowersQuery request, CancellationToken cancellationToken)
    {
        if (!await database.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
            return AppErrors.NotFound("User", request.UserId);

        if (!await FollowVisibility.CanViewFollowersAsync(database, request.ViewerId, request.UserId, cancellationToken))
            return AppErrors.Forbidden("User", request.UserId);

        var viewerId = request.ViewerId;

        return await database.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowedId == request.UserId)
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id)
            .Select(f => new UserSummaryResponse(
                f.Follower.Id,
                f.Follower.Name,
                f.Follower.FirstName,
                f.Follower.SecondName,
                f.Follower.ProfilePictureUrl,
                viewerId != null && database.UserFollows.Any(v => v.FollowerId == viewerId && v.FollowedId == f.FollowerId)))
            .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
    }
}
