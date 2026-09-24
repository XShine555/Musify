namespace Musify.Application.Events
{
    public record DeleteAlbumEvent(
        Guid AlbumId,
        long UserId);
}
