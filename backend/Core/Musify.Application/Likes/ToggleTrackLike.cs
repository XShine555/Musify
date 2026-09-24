using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Likes;

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
            .AnyAsync(t => t.Id == request.TrackId && t.LifeCycleStatus == LifeCycleStatus.Active, cancellationToken);
        if (!trackExists)
            return AppErrors.NotFound("Track", request.TrackId);

        var existingLike = await database.TrackLikes
            .SingleOrDefaultAsync(like => like.UserId == request.UserId && like.TrackId == request.TrackId, cancellationToken);

        bool nowLiked;
        if (existingLike != null)
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

        // A concurrent double click can hit the unique index; the outcome is already in place, so treat it as a no-op.
        var saved = await database.TrySaveChangesAsync(
            AppErrors.Conflict("TrackLike.Duplicate", "Track is already liked."), cancellationToken);
        if (saved.IsError)
            return true;

        logger.LogInformation("User {UserId} {Action} track {TrackId}",
            request.UserId, nowLiked ? "liked" : "unliked", request.TrackId);

        return nowLiked;
    }
}
