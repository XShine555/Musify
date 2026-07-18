using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using X.PagedList;
using X.PagedList.EF;
using Musify.Application.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks
{
    public record GetTracksQuery(string? Name, int PageNumber, int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse> >>;

    public class GetTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>> >
    {
        public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse> >> Handle(GetTracksQuery request, CancellationToken cancellationToken)
        {
            var tracksQuery = database.UserHasTracks
                .AsNoTracking()
                .Include(ut => ((ExternalTrack)ut.Track).TrackArtists)
                    .ThenInclude(trackArtist => trackArtist.Artist)
                .Include(ut => ((LocalTrack)ut.Track).Owner)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Name))
            {
                var normalizedName = request.Name.Trim().ToUpperInvariant();
                tracksQuery = tracksQuery.Where(t => t.Track.NormalizedTitle.Contains(normalizedName));
            }

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedEntities = await tracksQuery
                .OrderBy(t => t.Id)
                .Select(t => new { t.Track, ListensCount = t.Track.ListeningHistories.Count })
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            var pagedTracks = new StaticPagedList<TrackApplicationResponse>(
                pagedEntities.Select(x => TrackApplicationResponse.FromEntity(x.Track, x.ListensCount)),
                pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

            return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
        }
    }
}
