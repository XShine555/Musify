using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
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
            {
                logger.LogInformation("User {FollowedId} not found", request.FollowedId);
                return Error.NotFound(description: $"User {request.FollowedId} not found");
            }

            var alreadyFollowing = await database.UserFollows
                .AsNoTracking()
                .AnyAsync(f => f.FollowerId == request.FollowerId && f.FollowedId == request.FollowedId, cancellationToken);
            if (alreadyFollowing)
                return new Success();

            await database.UserFollows.AddAsync(new UserFollow
            {
                FollowerId = request.FollowerId,
                FollowedId = request.FollowedId
            }, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {FollowerId} followed user {FollowedId}", request.FollowerId, request.FollowedId);

            return new Success();
        }
    }
}
