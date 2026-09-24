using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record GetTracksByUserIdQuery(
        long UserId,
        string? Name,
        int PageNumber,
        int PageSize,
        IReadOnlyList<ProcessingStatus>? Statuses = null,
        long? ViewerId = null,
        bool? IsPublished = null)
        : IQuery<ErrorOr<PaginatedResponse<UserTrackApplicationResponse>>>;

    public class GetTracksByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksByUserIdQuery, ErrorOr<PaginatedResponse<UserTrackApplicationResponse>>>
    {
        public async ValueTask<ErrorOr<PaginatedResponse<UserTrackApplicationResponse>>> Handle(GetTracksByUserIdQuery request, CancellationToken cancellationToken)
        {
            var tracksQuery = database.Tracks
                .AsNoTracking()
                .Active()
                .Where(track => track.OwnerUserId == request.UserId);

            if (request.ViewerId != request.UserId)
                tracksQuery = tracksQuery.WherePublished();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var normalizedName = TextNormalizer.Normalize(request.Name);
                tracksQuery = tracksQuery.Where(track => track.NormalizedTitle.Contains(normalizedName));
            }

            if (request.Statuses is { Count: > 0 } statuses)
            {
                var statusList = statuses.Distinct().ToList();
                tracksQuery = tracksQuery.Where(track => statusList.Contains(track.Audio.TranscodeStatus));
            }

            if (request.IsPublished is { } isPublished)
                tracksQuery = tracksQuery.Where(track => track.IsPublished == isPublished);

            return await tracksQuery
                .OrderByDescending(track => track.CreatedAt)
                .ThenBy(track => track.Id)
                .SelectUserResponse()
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
