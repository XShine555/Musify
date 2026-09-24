using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Likes
{
    public record GetLikedTracksQuery(
        long UserId,
        int PageNumber,
        int PageSize)
        : IQuery<PaginatedResponse<TrackApplicationResponse>>;

    public class GetLikedTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetLikedTracksQuery, PaginatedResponse<TrackApplicationResponse>>
    {
        public async ValueTask<PaginatedResponse<TrackApplicationResponse>> Handle(GetLikedTracksQuery request, CancellationToken cancellationToken)
        {
            return await database.TrackLikes
                .AsNoTracking()
                .Where(like => like.UserId == request.UserId && like.Track.LifeCycleStatus == LifeCycleStatus.Active)
                .OrderByDescending(like => like.CreatedAt)
                .ThenBy(like => like.Id)
                .Select(like => like.Track)
                .SelectResponse()
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
