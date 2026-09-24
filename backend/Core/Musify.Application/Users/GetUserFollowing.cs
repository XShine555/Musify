using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users
{
    public record GetUserFollowingQuery(long? ViewerId, long UserId, int PageNumber, int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<UserSummaryResponse>>>;

    public class GetUserFollowingQueryHandler(IDatabase database)
        : IQueryHandler<GetUserFollowingQuery, ErrorOr<PaginatedResponse<UserSummaryResponse>>>
    {
        public async ValueTask<ErrorOr<PaginatedResponse<UserSummaryResponse>>> Handle(GetUserFollowingQuery request, CancellationToken cancellationToken)
        {
            if (!await database.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
                return AppErrors.NotFound("User", request.UserId);

            var viewerId = request.ViewerId;

            return await database.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == request.UserId)
                .OrderByDescending(f => f.CreatedAt)
                .ThenBy(f => f.Id)
                .Select(f => new UserSummaryResponse(
                    f.Followed.Id,
                    f.Followed.Name,
                    f.Followed.FirstName,
                    f.Followed.SecondName,
                    f.Followed.ProfilePictureUrl,
                    viewerId != null && database.UserFollows.Any(v => v.FollowerId == viewerId && v.FollowedId == f.FollowedId)))
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
