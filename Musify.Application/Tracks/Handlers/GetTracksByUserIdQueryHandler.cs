using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Pagination;
using Musify.Application.Tracks.Queries;
using Musify.Application.Tracks.Responses;
using X.PagedList.EF;
using Musify.Application.Contracts;

namespace Musify.Application.Tracks.Handlers
{
    public class GetTracksByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksByUserIdQuery, Result<PaginatedResponse<TrackApplicationResponse>> >
    {
        public async ValueTask<Result<PaginatedResponse<TrackApplicationResponse> >> Handle(GetTracksByUserIdQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .Include(ut => ut.Track)
                .Where(p => p.UserId == request.UserId);

            if (request.Name is not null)
            {
                var normalizedName = request.Name.Trim().ToUpperInvariant();
                tracksQuery = tracksQuery.Where(t => t.Track.NormalizedTitle.Contains(normalizedName));
            }

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedTracks = await tracksQuery
                .OrderBy(t => t.Track.CreatedAt)
                .Select(t => TrackApplicationResponse.FromEntity(t.Track))
                .ToPagedListAsync(pageNumber, pageSize, totalCount, cancellationToken);

            return Result.Success(PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks));
        }
    }
}