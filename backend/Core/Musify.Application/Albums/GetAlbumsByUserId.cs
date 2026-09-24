using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using X.PagedList;
using X.PagedList.EF;

namespace Musify.Application.Albums;

public record GetAlbumsByUserIdQuery(long UserId, int PageNumber, int PageSize)
    : IQuery<ErrorOr<PaginatedResponse<AlbumApplicationResponse>>>;

public class GetAlbumsByUserIdQueryHandler(IDatabase database)
    : IQueryHandler<GetAlbumsByUserIdQuery, ErrorOr<PaginatedResponse<AlbumApplicationResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<AlbumApplicationResponse>>> Handle(GetAlbumsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var userExists = await database.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);
        if (!userExists)
            return Error.NotFound(description: $"User {request.UserId} not found");

        var albumsQuery = database.Albums
            .AsNoTracking()
            .Where(album => album.OwnerUserId == request.UserId);

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

        var pagedAlbums = new StaticPagedList<AlbumApplicationResponse>(
            pagedEntities.Select(entry => AlbumApplicationResponse.FromEntity(entry.Album, entry.TrackCount, entry.CoverTrackIds)),
            pagedEntities.PageNumber, pagedEntities.PageSize, pagedEntities.TotalItemCount);

        return PaginatedResponse<AlbumApplicationResponse>.FromPagedList(pagedAlbums);
    }
}
