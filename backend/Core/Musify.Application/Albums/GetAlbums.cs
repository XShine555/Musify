using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using X.PagedList.EF;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums;

public record GetAlbumsQuery(string? Title, int PageNumber, int PageSize)
    : IQuery<ErrorOr<AlbumsSearchResponse>>;

public class GetAlbumsQueryHandler(IDatabase database)
    : IQueryHandler<GetAlbumsQuery, ErrorOr<AlbumsSearchResponse>>
{
    public async ValueTask<ErrorOr<AlbumsSearchResponse>> Handle(GetAlbumsQuery request, CancellationToken cancellationToken)
    {
        var albumsQuery = database.Albums
            .AsNoTracking()
            .Where(album => album.LifeCycleStatus == LifeCycleStatus.Active);

        if (!string.IsNullOrEmpty(request.Title))
        {
            var normalizedTitle = request.Title.Trim().ToUpperInvariant();
            albumsQuery = albumsQuery.Where(album => album.NormalizedTitle.Contains(normalizedTitle));
        }

        var totalCount = await albumsQuery.CountAsync(cancellationToken);

        var pagedEntities = await albumsQuery
            .OrderByDescending(album => album.CreatedAt)
            .Select(album => new
            {
                Album = album,
                TrackCount = album.AlbumTracks.Count,
                CoverTrackIds = album.AlbumTracks
                    .OrderBy(albumTrack => albumTrack.TrackNumber)
                    .Take(AlbumApplicationResponse.CoverTrackCount)
                    .Select(albumTrack => albumTrack.TrackId)
                    .ToList()
            })
            .ToPagedListAsync(request.PageNumber, request.PageSize, totalCount, cancellationToken);

        var items = pagedEntities
            .Select(entry => AlbumSearchItemResponse.FromAlbum(
                AlbumApplicationResponse.FromEntity(entry.Album, entry.TrackCount, entry.CoverTrackIds)))
            .ToList();

        return new AlbumsSearchResponse(
            items,
            pagedEntities.PageNumber,
            pagedEntities.PageSize,
            pagedEntities.PageCount,
            pagedEntities.TotalItemCount,
            pagedEntities.HasPreviousPage,
            pagedEntities.HasNextPage);
    }
}
