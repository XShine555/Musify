using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;

namespace Musify.Application.Users;

public record UnfollowUserCommand(long FollowerId, long FollowedId) : ICommand<ErrorOr<Success>>;

public class UnfollowUserCommandHandler(
    IDatabase database,
    ILogger<UnfollowUserCommandHandler> logger)
    : ICommandHandler<UnfollowUserCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
    {
        var follow = await database.UserFollows
            .SingleOrDefaultAsync(f => f.FollowerId == request.FollowerId && f.FollowedId == request.FollowedId, cancellationToken);

        if (follow == null)
            return new Success();

        database.UserFollows.Remove(follow);
        await database.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {FollowerId} unfollowed user {FollowedId}", request.FollowerId, request.FollowedId);

        return new Success();
    }
}
