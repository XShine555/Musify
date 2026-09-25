using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Coauthors
{
    public record RemoveCoauthorCommand(long UserId, Guid TrackId, long CoauthorUserId)
        : ICommand<ErrorOr<Success>>;

    public class RemoveCoauthorCommandHandler(
        IDatabase database,
        ILogger<RemoveCoauthorCommandHandler> logger)
        : ICommandHandler<RemoveCoauthorCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(RemoveCoauthorCommand request, CancellationToken cancellationToken)
        {
            var ownerUserId = await database.Tracks
                .AsNoTracking()
                .Where(track => track.Id == request.TrackId && track.LifeCycleStatus == LifeCycleStatus.Active)
                .Select(track => (long?)track.OwnerUserId)
                .SingleOrDefaultAsync(cancellationToken);

            if (ownerUserId == null)
                return AppErrors.NotFound("Track", request.TrackId);

            if (ownerUserId != request.UserId && request.CoauthorUserId != request.UserId)
                return AppErrors.Forbidden("Track", request.TrackId);

            var coauthor = await database.TrackCoauthors
                .SingleOrDefaultAsync(c => c.TrackId == request.TrackId && c.UserId == request.CoauthorUserId, cancellationToken);
            if (coauthor == null)
                return AppErrors.NotFound("Coauthor", request.CoauthorUserId);

            database.TrackCoauthors.Remove(coauthor);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} removed coauthor {CoauthorUserId} from track {TrackId}",
                request.UserId, request.CoauthorUserId, request.TrackId);

            return Result.Success;
        }
    }
}
