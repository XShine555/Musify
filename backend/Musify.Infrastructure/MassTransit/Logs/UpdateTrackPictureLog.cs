namespace Musify.Infrastructure.MassTransit.Logs
{
    public record UpdateTrackPictureLog(
        Guid TrackId,
        string PreviousOriginalPictureKey,
    string? PreviousSmallPictureKey,
    string? PreviousMediumPictureKey,
    string? PreviousLargePictureKey);
}
