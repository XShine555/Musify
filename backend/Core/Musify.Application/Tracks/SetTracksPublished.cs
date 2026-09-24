using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record SetTracksPublishedCommand(long UserId, IReadOnlyList<Guid> TrackIds, bool IsPublished)
        : ICommand<ErrorOr<SetTracksPublishedResponse>>;

    public class SetTracksPublishedCommandHandler(
        IDatabase database,
        ILogger<SetTracksPublishedCommandHandler> logger)
        : ICommandHandler<SetTracksPublishedCommand, ErrorOr<SetTracksPublishedResponse>>
    {
        public async ValueTask<ErrorOr<SetTracksPublishedResponse>> Handle(SetTracksPublishedCommand request, CancellationToken cancellationToken)
        {
            var requestedIds = request.TrackIds.Distinct().ToList();

            var tracks = await database.Tracks
                .Where(track => requestedIds.Contains(track.Id) && track.LifeCycleStatus == LifeCycleStatus.Active)
                .ToListAsync(cancellationToken);

            var foreign = tracks.FirstOrDefault(track => track.OwnerUserId != request.UserId);
            if (foreign != null)
                return AppErrors.Forbidden("Track", foreign.Id);

            var updated = new List<Guid>();
            var skipped = new List<Guid>();

            foreach (var trackId in requestedIds)
            {
                var track = tracks.SingleOrDefault(candidate => candidate.Id == trackId);
                if (track == null || (request.IsPublished && !track.Audio.IsProcessed))
                {
                    skipped.Add(trackId);
                    continue;
                }

                track.IsPublished = request.IsPublished;
                updated.Add(trackId);
            }

            if (updated.Count > 0)
            {
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("{Action} {Count} tracks of user {UserId}",
                    request.IsPublished ? "Published" : "Unpublished", updated.Count, request.UserId);
            }

            return new SetTracksPublishedResponse(updated, skipped);
        }
    }
}
