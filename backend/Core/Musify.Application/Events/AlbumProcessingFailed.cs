namespace Musify.Application.Events
{
    public record AlbumProcessingFailed(
        Guid AlbumId,
        string? Bucket,
        string? PictureKey);
}
