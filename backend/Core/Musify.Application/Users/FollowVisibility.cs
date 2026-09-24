using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;

namespace Musify.Application.Users;

public static class FollowVisibility
{
    public static async Task<bool> CanViewFollowersAsync(
        IDatabase database,
        long? viewerId,
        long userId,
        CancellationToken cancellationToken)
    {
        if (viewerId is not { } viewer)
            return false;

        if (viewer == userId)
            return true;

        var mutualRows = await database.UserFollows
            .AsNoTracking()
            .CountAsync(f =>
                (f.FollowerId == viewer && f.FollowedId == userId) ||
                (f.FollowerId == userId && f.FollowedId == viewer),
                cancellationToken);

        return mutualRows == 2;
    }
}
