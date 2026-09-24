using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.Likes;

public record GetLikedTracksQuery(
    long UserId,
    int PageNumber,
    int PageSize)
    : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;

public class GetLikedTracksQueryHandler(IDatabase database)
    : IQueryHandler<GetLikedTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse>>> Handle(GetLikedTracksQuery request, CancellationToken cancellationToken)
    {
        var likesQuery = database.TrackLikes
            .AsNoTracking()
            .Include(like => like.Track.Owner)
            .Include(like => like.Track.Tags)
            .Where(like => like.UserId == request.UserId);

        var totalCount = await likesQuery.CountAsync(cancellationToken);

        var pagedEntities = await likesQuery
            .OrderByDescending(like => like.CreatedAt)
            .Select(like => new { like.Track, ListensCount = like.Track.ListeningHistories.Count(l => l.IsCounted) })
            .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

        var pagedTracks = new StaticPagedList<TrackApplicationResponse>(
            pagedEntities.Select(x => TrackApplicationResponse.FromEntity(x.Track, x.ListensCount)),
            pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

        return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
    }
}
