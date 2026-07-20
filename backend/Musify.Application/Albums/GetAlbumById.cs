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
            var album = await database.UserAlbums
                .AsNoTracking()
                .Where(a => a.Id == request.Id)
                .Select(a => new { Album = a, TrackCount = a.AlbumTracks.Count })
                .SingleOrDefaultAsync(cancellationToken);

            if (album is null)
                return Error.NotFound();

            return AlbumApplicationResponse.FromEntity(album.Album, album.TrackCount);
        }
    }
}
