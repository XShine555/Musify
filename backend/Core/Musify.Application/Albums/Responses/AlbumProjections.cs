using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums.Responses;

public static class AlbumProjections
{
    public const int CoverTrackCount = 4;

    public static IQueryable<Album> Active(this IQueryable<Album> query) =>
        query.Where(album => album.LifeCycleStatus == LifeCycleStatus.Active);

    public static IQueryable<AlbumApplicationResponse> SelectResponse(this IQueryable<Album> query) =>
        query.Select(album => new AlbumApplicationResponse(
            album.Id,
            album.Title,
            album.Description,
            album.ReleaseYear,
            album.OwnerUserId,
            album.AlbumTracks.Count,
            album.Pictures!.SmallName,
            album.Pictures!.MediumName,
            album.Pictures!.LargeName,
            album.CreatedAt,
            album.UpdatedAt,
            album.AlbumTracks
                .OrderBy(albumTrack => albumTrack.TrackNumber)
                .Take(CoverTrackCount)
                .Select(albumTrack => albumTrack.TrackId)
                .ToList()));
}
