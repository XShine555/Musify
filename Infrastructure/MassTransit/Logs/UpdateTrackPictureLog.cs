namespace Musify.Infrastructure.MassTransit.Activities.Logs
{
    public record UpdateTrackPictureLog(
        Guid TrackId,
        string PreviousOriginalPictureKeyName,
        string PreviousSmallPictureKeyName,
        string PreviousMediumPictureKeyName,
        string PreviousLargePictureKeyName);
}
