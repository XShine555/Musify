using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Coauthors
{
    public record InviteCoauthorsCommand(long OwnerId, Guid TrackId, IReadOnlyList<long> UserIds)
        : ICommand<ErrorOr<IReadOnlyList<TrackCoauthorInviteReference>>>;

    public class InviteCoauthorsCommandHandler(
        IDatabase database,
        ILogger<InviteCoauthorsCommandHandler> logger)
        : ICommandHandler<InviteCoauthorsCommand, ErrorOr<IReadOnlyList<TrackCoauthorInviteReference>>>
    {
        public async ValueTask<ErrorOr<IReadOnlyList<TrackCoauthorInviteReference>>> Handle(InviteCoauthorsCommand request, CancellationToken cancellationToken)
        {
            var found = await database.Tracks.FindOwnedAsync(request.TrackId, request.OwnerId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var userIds = request.UserIds.Distinct().ToList();
            if (userIds.Contains(request.OwnerId))
                return Error.Validation("Coauthor.Self", "You cannot invite yourself as a coauthor.");

            var existingUserIds = await database.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);

            var missing = userIds.Where(id => !existingUserIds.Contains(id)).Select(id => (long?)id).FirstOrDefault();
            if (missing != null)
                return AppErrors.NotFound("User", missing.Value);

            var existing = await database.TrackCoauthors
                .Where(coauthor => coauthor.TrackId == request.TrackId && userIds.Contains(coauthor.UserId))
                .ToListAsync(cancellationToken);

            var alreadyInvited = existing.FirstOrDefault(coauthor => coauthor.Status != CoauthorStatus.Rejected);
            if (alreadyInvited != null)
                return AppErrors.Conflict("Coauthor.AlreadyInvited", $"User {alreadyInvited.UserId} was already invited to this track.");

            var now = DateTime.UtcNow;
            foreach (var rejected in existing)
            {
                rejected.Status = CoauthorStatus.Pending;
                rejected.InvitedAt = now;
                rejected.RespondedAt = null;
            }

            var reinvitedIds = existing.Select(coauthor => coauthor.UserId).ToHashSet();
            foreach (var userId in userIds.Where(id => !reinvitedIds.Contains(id)))
            {
                await database.TrackCoauthors.AddAsync(new TrackCoauthor
                {
                    TrackId = request.TrackId,
                    UserId = userId,
                    InvitedAt = now
                }, cancellationToken);
            }

            var saved = await database.TrySaveChangesAsync(
                AppErrors.Conflict("Coauthor.AlreadyInvited", "A user was already invited to this track."), cancellationToken);
            if (saved.IsError)
                return saved.Errors;

            logger.LogInformation("Owner {OwnerId} invited {Count} coauthors to track {TrackId}", request.OwnerId, userIds.Count, request.TrackId);

            return await database.TrackCoauthors
                .AsNoTracking()
                .Where(coauthor => coauthor.TrackId == request.TrackId)
                .OrderBy(coauthor => coauthor.InvitedAt)
                .ThenBy(coauthor => coauthor.UserId)
                .Select(coauthor => new TrackCoauthorInviteReference(coauthor.UserId, coauthor.User.Name, coauthor.Status))
                .ToListAsync(cancellationToken);
        }
    }
}
