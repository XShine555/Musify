using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;

namespace Musify.Application.Users
{
    public record FollowUserCommand(long FollowerId, long FollowedId) : ICommand<ErrorOr<Success>>;

    public class FollowUserCommandHandler(
        IDatabase database,
        ILogger<FollowUserCommandHandler> logger)
        : ICommandHandler<FollowUserCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(FollowUserCommand request, CancellationToken cancellationToken)
        {
            if (request.FollowerId == request.FollowedId)
                return Error.Validation(description: "A user cannot follow themselves.");

            var followedExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.FollowedId, cancellationToken);
            if (!followedExists)
                return AppErrors.NotFound("User", request.FollowedId);

            var alreadyFollowing = await database.UserFollows
                .AsNoTracking()
                .AnyAsync(f => f.FollowerId == request.FollowerId && f.FollowedId == request.FollowedId, cancellationToken);
            if (alreadyFollowing)
                return Result.Success;

            await database.UserFollows.AddAsync(new UserFollow
            {
                FollowerId = request.FollowerId,
                FollowedId = request.FollowedId
            }, cancellationToken);

            // A concurrent double click can hit the unique index; already following is a successful no-op.
            var saved = await database.TrySaveChangesAsync(
                AppErrors.Conflict("UserFollow.Duplicate", "Already following this user."), cancellationToken);
            if (saved.IsError)
                return Result.Success;

            logger.LogInformation("User {FollowerId} followed user {FollowedId}", request.FollowerId, request.FollowedId);

            return Result.Success;
        }
    }
}
