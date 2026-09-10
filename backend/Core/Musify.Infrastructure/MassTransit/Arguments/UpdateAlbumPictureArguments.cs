namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record UpdateAlbumPictureArguments(
        Guid AlbumId,
        string OriginalPictureKey,
        string SmallPictureVariable,
        string MediumPictureVariable,
        string LargePictureVariable);
}
