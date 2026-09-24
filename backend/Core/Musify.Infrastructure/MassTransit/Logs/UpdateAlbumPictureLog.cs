namespace Musify.Infrastructure.MassTransit.Logs;

public record UpdateAlbumPictureLog(
    Guid AlbumId,
    string PreviousOriginalPictureKey,
    string PreviousSmallPictureKey,
    string PreviousMediumPictureKey,
    string PreviousLargePictureKey);
