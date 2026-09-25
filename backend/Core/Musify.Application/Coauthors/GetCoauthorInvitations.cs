using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Coauthors.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Coauthors
{
    public record GetCoauthorInvitationsQuery(long UserId, int PageNumber, int PageSize)
        : IQuery<PaginatedResponse<CoauthorInvitationResponse>>;

    public class GetCoauthorInvitationsQueryHandler(IDatabase database)
        : IQueryHandler<GetCoauthorInvitationsQuery, PaginatedResponse<CoauthorInvitationResponse>>
    {
        public async ValueTask<PaginatedResponse<CoauthorInvitationResponse>> Handle(GetCoauthorInvitationsQuery request, CancellationToken cancellationToken)
        {
            return await database.TrackCoauthors
                .AsNoTracking()
                .Where(coauthor => coauthor.UserId == request.UserId
                    && coauthor.Status == CoauthorStatus.Pending
                    && coauthor.Track.LifeCycleStatus == LifeCycleStatus.Active)
                .OrderByDescending(coauthor => coauthor.InvitedAt)
                .ThenBy(coauthor => coauthor.Id)
                .Select(coauthor => new CoauthorInvitationResponse(
                    coauthor.Id,
                    coauthor.TrackId,
                    coauthor.Track.Title,
                    coauthor.Track.Owner.Name,
                    coauthor.Track.OwnerUserId,
                    coauthor.InvitedAt))
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
