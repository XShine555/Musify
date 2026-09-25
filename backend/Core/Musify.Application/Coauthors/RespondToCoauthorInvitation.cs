using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Coauthors
{
    public record RespondToCoauthorInvitationCommand(long UserId, Guid InvitationId, bool Accept)
        : ICommand<ErrorOr<Success>>;

    public class RespondToCoauthorInvitationCommandHandler(
        IDatabase database,
        ILogger<RespondToCoauthorInvitationCommandHandler> logger)
        : ICommandHandler<RespondToCoauthorInvitationCommand, ErrorOr<Success>>
    {
        public async ValueTask<ErrorOr<Success>> Handle(RespondToCoauthorInvitationCommand request, CancellationToken cancellationToken)
        {
            var invitation = await database.TrackCoauthors
                .Include(coauthor => coauthor.Track)
                .SingleOrDefaultAsync(coauthor => coauthor.Id == request.InvitationId, cancellationToken);

            if (invitation == null || invitation.Track.LifeCycleStatus != LifeCycleStatus.Active)
                return AppErrors.NotFound("CoauthorInvitation", request.InvitationId);

            if (invitation.UserId != request.UserId)
                return AppErrors.Forbidden("CoauthorInvitation", request.InvitationId);

            if (invitation.Status != CoauthorStatus.Pending)
                return AppErrors.Conflict("CoauthorInvitation.NotPending", "The invitation was already answered.");

            invitation.Status = request.Accept ? CoauthorStatus.Accepted : CoauthorStatus.Rejected;
            invitation.RespondedAt = DateTime.UtcNow;
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} {Action} coauthor invitation {InvitationId}",
                request.UserId, request.Accept ? "accepted" : "rejected", request.InvitationId);

            return Result.Success;
        }
    }
}
