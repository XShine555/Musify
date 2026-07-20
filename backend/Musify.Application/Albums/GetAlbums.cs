using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.Albums
{
    public record GetAlbumsQuery(string? Title, int PageNumber, int PageSize)
        : IQuery<ErrorOr<PaginatedResponse<AlbumApplicationResponse>>>;

    public class GetAlbumsQueryHandler(IDatabase database)
        : IQueryHandler<GetAlbumsQuery, ErrorOr<PaginatedResponse<AlbumApplicationResponse>>>
    {
        public async ValueTask<ErrorOr<PaginatedResponse<AlbumApplicationResponse>>> Handle(GetAlbumsQuery request, CancellationToken cancellationToken)
        {
            var albumsQuery = database.UserAlbums
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Title))
            {
                var normalizedTitle = request.Title.Trim().ToUpperInvariant();
                albumsQuery = albumsQuery.Where(album => album.NormalizedTitle.Contains(normalizedTitle));
            }

            var totalCount = await albumsQuery.CountAsync(cancellationToken);

            var pagedEntities = await albumsQuery
                .OrderByDescending(album => album.CreatedAt)
                .Select(album => new { Album = album, TrackCount = album.AlbumTracks.Count })
                .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

            var pagedAlbums = new StaticPagedList<AlbumApplicationResponse>(
                pagedEntities.Select(entry => AlbumApplicationResponse.FromEntity(entry.Album, entry.TrackCount)),
                pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

            return PaginatedResponse<AlbumApplicationResponse>.FromPagedList(pagedAlbums);
        }
    }
}
