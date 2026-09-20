using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;

namespace Musify.Application.Albums
{
    public record GetAlbumByIdQuery(Guid Id)
        : IQuery<ErrorOr<AlbumApplicationResponse>>;

    public class GetAlbumByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetAlbumByIdQuery, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(GetAlbumByIdQuery request, CancellationToken cancellationToken)
        {
            var album = await database.Albums
                .AsNoTracking()
                .Where(a => a.Id == request.Id)
                .Select(a => new
                {
                    Album = a,
                    TrackCount = a.AlbumTracks.Count,
                    CoverTrackIds = a.AlbumTracks
                        .OrderBy(albumTrack => albumTrack.TrackNumber)
                        .Take(AlbumApplicationResponse.CoverTrackCount)
                        .Select(albumTrack => albumTrack.TrackId)
                        .ToList()
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (album == null)
                return Error.NotFound();

            return AlbumApplicationResponse.FromEntity(album.Album, album.TrackCount, album.CoverTrackIds);
        }
    }
}
