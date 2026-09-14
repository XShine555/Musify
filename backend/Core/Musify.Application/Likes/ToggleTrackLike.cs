using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Likes
{
    public record ToggleTrackLikeCommand(
        long UserId,
        Guid? TrackId,
        TrackSource? Source,
        string? ExternalId)
        : ICommand<ErrorOr<bool>>;

    public class ToggleTrackLikeCommandHandler(
        IDatabase database,
        YouTubeTrackProvisioner provisioner,
        ILogger<ToggleTrackLikeCommandHandler> logger)
        : ICommandHandler<ToggleTrackLikeCommand, ErrorOr<bool>>
    {
        public async ValueTask<ErrorOr<bool>> Handle(ToggleTrackLikeCommand request, CancellationToken cancellationToken)
        {
            var trackIdResult = await ResolveTrackIdAsync(request, cancellationToken);
            if (trackIdResult.IsError)
                return trackIdResult.Errors;

            var trackId = trackIdResult.Value;

            var existingLike = await database.TrackLikes
                .SingleOrDefaultAsync(like => like.UserId == request.UserId && like.TrackId == trackId, cancellationToken);

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
                    TrackId = trackId
                }, cancellationToken);
                nowLiked = true;
            }

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} {Action} track {TrackId}",
                request.UserId, nowLiked ? "liked" : "unliked", trackId);

            return nowLiked;
        }

        private async ValueTask<ErrorOr<Guid>> ResolveTrackIdAsync(ToggleTrackLikeCommand request, CancellationToken cancellationToken)
        {
            if (request.TrackId.HasValue)
            {
                var trackExists = await database.Tracks
                    .AsNoTracking()
                    .AnyAsync(t => t.Id == request.TrackId.Value, cancellationToken);
                if (!trackExists)
                {
                    logger.LogInformation("Track {TrackId} not found", request.TrackId);
                    return Error.NotFound(description: $"Track {request.TrackId} not found");
                }

                return request.TrackId.Value;
            }

            var provisionResult = await provisioner.GetOrCreateAsync(request.ExternalId!, cancellationToken);
            if (provisionResult.IsError)
                return provisionResult.Errors;

            return provisionResult.Value.Track.Id;
        }
    }
}
