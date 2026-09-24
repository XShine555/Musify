using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

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
                .Active()
                .AnyAsync(album => album.Id == request.AlbumId, cancellationToken);
            if (!albumExists)
                return AppErrors.NotFound("Album", request.AlbumId);

            return await database.AlbumHasTracks
                .AsNoTracking()
                .Where(albumTrack => albumTrack.AlbumId == request.AlbumId && albumTrack.Track.LifeCycleStatus == Domain.ValueObjects.LifeCycleStatus.Active)
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .ThenBy(albumTrack => albumTrack.Id)
                .Select(albumTrack => albumTrack.Track)
                .SelectResponse()
                .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
        }
    }
}
