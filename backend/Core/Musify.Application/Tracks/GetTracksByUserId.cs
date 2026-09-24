using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks
{
    public record GetTracksByUserIdQuery(
        long UserId,
        string? Name,
        int PageNumber,
        int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;

    public class GetTracksByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksByUserIdQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>>>
    {
        public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse>>> Handle(GetTracksByUserIdQuery request, CancellationToken cancellationToken)
        {
            var tracksQuery = database.Tracks
                .AsNoTracking()
                .Active()
                .Where(track => track.OwnerUserId == request.UserId);

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var normalizedName = TextNormalizer.Normalize(request.Name);
                tracksQuery = tracksQuery.Where(track => track.NormalizedTitle.Contains(normalizedName));
            }

            return await tracksQuery
                .OrderByDescending(track => track.CreatedAt)
                .ThenBy(track => track.Id)
                .SelectResponse()
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
