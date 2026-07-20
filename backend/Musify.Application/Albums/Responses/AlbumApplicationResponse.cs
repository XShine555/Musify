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
        DateTime UpdatedAt)
    {
        public static AlbumApplicationResponse FromEntity(UserAlbum album, int trackCount)
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
                album.UpdatedAt);
        }
    }
}
