using Musify.Domain.Entities;

namespace Musify.Application.Albums.Responses
{
    public record ExternalAlbumApplicationResponse(
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
        string ExternalId,
        string? ThumbnailUrl)
        : AlbumApplicationResponse(
            Id, Title, Description, ReleaseYear, OwnerUserId, TrackCount,
            SmallImageKeyName, MediumImageKeyName, LargeImageKeyName,
            CreatedAt, UpdatedAt, CoverTrackIds)
    {
        public static ExternalAlbumApplicationResponse FromEntity(
            ExternalAlbum album,
            int trackCount,
            IReadOnlyList<Guid>? coverTrackIds = null)
        {
            return new ExternalAlbumApplicationResponse(
                album.Id,
                album.Title,
                album.Description,
                album.ReleaseYear,
                0,
                trackCount,
                album.Pictures?.SmallName,
                album.Pictures?.MediumName,
                album.Pictures?.LargeName,
                album.CreatedAt,
                album.UpdatedAt,
                coverTrackIds ?? [],
                album.ExternalId,
                album.ThumbnailUrl);
        }
    }
}
