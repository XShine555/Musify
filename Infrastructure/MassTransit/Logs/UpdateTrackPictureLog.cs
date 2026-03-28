namespace Musify.Infrastructure.MassTransit.Activities.Logs
{
    public record UpdateTrackPictureLog(
        Guid TrackId,
        string PreviousOriginalPictureKey,
        string PreviousSmallPictureKey,
        string PreviousMediumPictureKey,
        string PreviousLargePictureKey);
}
