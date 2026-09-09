using Musify.Domain.Entities;

namespace Musify.Application.Albums.Responses
{
    public record AlbumApplicationResponse(
        Guid Id,
        string Title,
        string? Description,
        int? ReleaseYear,
        long OwnerUserId,
        int TrackCount,
        string? SmallImageKeyName,
        string? MediumImageKeyName,
        string? LargeImageKeyName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<Guid> CoverTrackIds,
        string? YouTubeAlbumId = null,
        string? ThumbnailUrl = null)
    {
        public const int CoverTrackCount = 4;

        public static AlbumApplicationResponse FromEntity(
            UserAlbum album,
            int trackCount,
            IReadOnlyList<Guid>? coverTrackIds = null)
        {
            return new AlbumApplicationResponse(
                album.Id,
                album.Title,
                album.Description,
                album.ReleaseYear,
                album.OwnerUserId,
                trackCount,
                album.Pictures?.SmallName,
                album.Pictures?.MediumName,
                album.Pictures?.LargeName,
                album.CreatedAt,
                album.UpdatedAt,
                coverTrackIds ?? []);
        }

        public static AlbumApplicationResponse FromEntity(
            Album album,
            int trackCount,
            IReadOnlyList<Guid>? coverTrackIds = null)
        {
            var externalAlbum = album as ExternalAlbum;
            return new AlbumApplicationResponse(
                album.Id,
                album.Title,
                album.Description,
                album.ReleaseYear,
                album is UserAlbum userAlbum ? userAlbum.OwnerUserId : 0,
                trackCount,
                album.Pictures?.SmallName,
                album.Pictures?.MediumName,
                album.Pictures?.LargeName,
                album.CreatedAt,
                album.UpdatedAt,
                coverTrackIds ?? [],
                externalAlbum?.ExternalId,
                externalAlbum?.ThumbnailUrl);
        }
    }
}
