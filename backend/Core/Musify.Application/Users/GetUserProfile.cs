using System.Text.Json.Serialization;
using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Serialization;

namespace Musify.Application.Users;

public record GetUserProfileQuery(long UserId, long? ViewerId = null) : IQuery<ErrorOr<UserProfileResponse>>;

public record UserProfileResponse(
    [property: JsonConverter(typeof(LongAsStringConverter))] long Id,
    string Name,
    string? FirstName,
    string? SecondName,
    string? ProfilePictureUrl,
    DateTime CreatedAt,
    int FollowersCount,
    int FollowingCount,
    bool IsFollowing,
    bool CanViewFollowers);

public class GetUserProfileQueryHandler(IDatabase database)
    : IQueryHandler<GetUserProfileQuery, ErrorOr<UserProfileResponse>>
{
    public async ValueTask<ErrorOr<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await database.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.FirstName,
                u.SecondName,
                u.ProfilePictureUrl,
                u.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user == null)
            return Error.NotFound();

        var followersCount = await database.UserFollows
            .AsNoTracking()
            .CountAsync(f => f.FollowedId == request.UserId, cancellationToken);

        var followingCount = await database.UserFollows
            .AsNoTracking()
            .CountAsync(f => f.FollowerId == request.UserId, cancellationToken);

        var isFollowing = request.ViewerId is { } viewerId && viewerId != request.UserId &&
            await database.UserFollows
                .AsNoTracking()
                .AnyAsync(f => f.FollowerId == viewerId && f.FollowedId == request.UserId, cancellationToken);

        var canViewFollowers = await FollowVisibility.CanViewFollowersAsync(
            database, request.ViewerId, request.UserId, cancellationToken);

        return new UserProfileResponse(
            user.Id,
            user.Name,
            user.FirstName,
            user.SecondName,
            user.ProfilePictureUrl,
            user.CreatedAt,
            followersCount,
            followingCount,
            isFollowing,
            canViewFollowers);
    }
}
