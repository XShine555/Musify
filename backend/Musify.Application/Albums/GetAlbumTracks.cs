using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.Albums
{
    public record GetAlbumTracksQuery(
        Guid AlbumId,
        int PageNumber,
        int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<TrackApplicationResponse>>>;

    public class GetAlbumTracksQueryHandler(IDatabase database)
        : IQueryHandler<GetAlbumTracksQuery, ErrorOr<PaginatedResponse<TrackApplicationResponse>>>
    {
        public async ValueTask<ErrorOr<PaginatedResponse<TrackApplicationResponse>>> Handle(GetAlbumTracksQuery request, CancellationToken cancellationToken)
        {
            var albumExists = await database.Albums
                .AsNoTracking()
                .AnyAsync(album => album.Id == request.AlbumId, cancellationToken);
            if (!albumExists)
                return Error.NotFound();

            var tracksQuery = database.AlbumHasTracks
                .AsNoTracking()
                .Include(albumTrack => ((ExternalTrack)albumTrack.Track).TrackArtists)
                    .ThenInclude(trackArtist => trackArtist.Artist)
                .Include(albumTrack => ((LocalTrack)albumTrack.Track).Owner)
                .Where(albumTrack => albumTrack.AlbumId == request.AlbumId);

            var totalCount = await tracksQuery.CountAsync(cancellationToken);

            var pagedEntities = await tracksQuery
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .Select(albumTrack => new { albumTrack.Track, ListensCount = albumTrack.Track.ListeningHistories.Count })
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            var pagedTracks = new StaticPagedList<TrackApplicationResponse>(
                pagedEntities.Select(entry => TrackApplicationResponse.FromEntity(entry.Track, entry.ListensCount)),
                pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

            return PaginatedResponse<TrackApplicationResponse>.FromPagedList(pagedTracks);
        }
    }
}
