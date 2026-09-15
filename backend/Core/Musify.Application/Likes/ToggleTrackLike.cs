using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Likes
{
    public record ToggleTrackLikeCommand(long UserId, Guid TrackId)
        : ICommand<ErrorOr<bool>>;

    public class ToggleTrackLikeCommandHandler(
        IDatabase database,
        ILogger<ToggleTrackLikeCommandHandler> logger)
        : ICommandHandler<ToggleTrackLikeCommand, ErrorOr<bool>>
    {
        public async ValueTask<ErrorOr<bool>> Handle(ToggleTrackLikeCommand request, CancellationToken cancellationToken)
        {
            var trackExists = await database.Tracks
                .AsNoTracking()
                .AnyAsync(t => t.Id == request.TrackId, cancellationToken);
            if (!trackExists)
            {
                logger.LogInformation("Track {TrackId} not found", request.TrackId);
                return Error.NotFound(description: $"Track {request.TrackId} not found");
            }

            var existingLike = await database.TrackLikes
                .SingleOrDefaultAsync(like => like.UserId == request.UserId && like.TrackId == request.TrackId, cancellationToken);

            bool nowLiked;
            if (existingLike is not null)
            {
                database.TrackLikes.Remove(existingLike);
                nowLiked = false;
            }
            else
            {
                await database.TrackLikes.AddAsync(new TrackLike
                {
                    UserId = request.UserId,
                    TrackId = request.TrackId
                }, cancellationToken);
                nowLiked = true;
            }

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} {Action} track {TrackId}",
                request.UserId, nowLiked ? "liked" : "unliked", request.TrackId);

            return nowLiked;
        }
    }
}
