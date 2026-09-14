using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;

namespace Musify.Application.Users
{
    public record IsFollowingUserQuery(long FollowerId, long FollowedId) : IQuery<bool>;

    public class IsFollowingUserQueryHandler(IDatabase database)
        : IQueryHandler<IsFollowingUserQuery, bool>
    {
        public ValueTask<bool> Handle(IsFollowingUserQuery request, CancellationToken cancellationToken) =>
            new(database.UserFollows
                .AsNoTracking()
                .AnyAsync(f => f.FollowerId == request.FollowerId && f.FollowedId == request.FollowedId, cancellationToken));
    }
}
