using Musify.Application.Shared;

namespace Musify.Application.Events
{
    public record UpdateAlbumPictureEvent(
        Guid AlbumId,
        string Bucket,
        string SourceKey,
        ImageSize Small,
        ImageSize Medium,
        ImageSize Large);
}
