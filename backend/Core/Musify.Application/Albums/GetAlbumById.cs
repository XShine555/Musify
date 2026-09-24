using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;

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
                .Active()
                .Where(a => a.Id == request.Id)
                .SelectResponse()
                .SingleOrDefaultAsync(cancellationToken);

            return album == null
                ? AppErrors.NotFound("Album", request.Id)
                : album;
        }
    }
}
