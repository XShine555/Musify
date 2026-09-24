using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record DeleteTracksCommand(long UserId, IReadOnlyList<Guid> TrackIds)
        : ICommand<ErrorOr<DeleteTracksResponse>>;

    public class DeleteTracksCommandHandler(
        IDatabase database,
        IEventBus eventBus,
        ILogger<DeleteTracksCommandHandler> logger)
        : ICommandHandler<DeleteTracksCommand, ErrorOr<DeleteTracksResponse>>
    {
        public async ValueTask<ErrorOr<DeleteTracksResponse>> Handle(DeleteTracksCommand request, CancellationToken cancellationToken)
        {
            var requestedIds = request.TrackIds.Distinct().ToList();

            var tracks = await database.Tracks
                .Where(track => requestedIds.Contains(track.Id) && track.LifeCycleStatus == LifeCycleStatus.Active)
                .ToListAsync(cancellationToken);

            var foreign = tracks.FirstOrDefault(track => track.OwnerUserId != request.UserId);
            if (foreign != null)
                return AppErrors.Forbidden("Track", foreign.Id);

            var deleted = new List<Guid>();
            var failed = new List<Guid>();

            foreach (var trackId in requestedIds)
            {
                var track = tracks.SingleOrDefault(candidate => candidate.Id == trackId);
                if (track == null || track.Audio.IsInProgress)
                {
                    failed.Add(trackId);
                    continue;
                }

                track.LifeCycleStatus = LifeCycleStatus.Removing;
                await eventBus.PublishAsync(new DeleteTrackEvent(track.Id, request.UserId), cancellationToken);
                deleted.Add(trackId);
            }

            if (deleted.Count > 0)
            {
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Marked {Count} tracks of user {UserId} as removing", deleted.Count, request.UserId);
            }

            return new DeleteTracksResponse(deleted, failed);
        }
    }
}
