using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Application;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;
using X.PagedList.EF;

namespace Musify.Application.Tracks.Handler
{
    public class GetTracksByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksByUserIdQuery, Result<PaginatedResponse<TrackResponse>> >
    {
        public async ValueTask<Result<PaginatedResponse<TrackResponse> >> Handle(GetTracksByUserIdQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var normalizedName = request.Name?.Trim().ToUpperInvariant() ?? string.Empty;

            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .Include(ut => ut.Track)
                .Where(p => p.UserId == request.UserId);

            if (!string.IsNullOrWhiteSpace(normalizedName))
                tracksQuery = tracksQuery.Where(t => t.Track.NormalizedTitle.Contains(normalizedName));

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedTracks = await tracksQuery
                .OrderBy(t => t.Track.CreatedDate)
                .Select(t => TrackResponse.FromEntity(t.Track))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            var response = new PaginatedResponse<TrackResponse>(
                pagedTracks.ToArray(),
                pagedTracks.PageNumber,
                pagedTracks.PageSize,
                pagedTracks.PageCount,
                pagedTracks.TotalItemCount,
                pagedTracks.HasNextPage,
                pagedTracks.HasPreviousPage);

            return Result.Success(response);
        }
    }
}