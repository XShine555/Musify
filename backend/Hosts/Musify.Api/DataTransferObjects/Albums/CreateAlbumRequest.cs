using Musify.Domain.ValueObjects;

namespace Musify.Api.DataTransferObjects.Albums
{
    public record CreateAlbumRequest(
        string Title,
        string? Description,
        int? ReleaseYear,
        Guid PictureIntentId,
        AlbumType Type = AlbumType.Album,
        IReadOnlyList<Guid>? TrackIds = null);
}
