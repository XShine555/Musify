namespace Musify.Infrastructure.Messaging.Activities.Logs
{
    public record UpdateTrackPictureLog(
        Guid TrackId,
        string? PreviousSmallPictureKeyName,
        string? PreviousMediumPictureKeyName,
        string? PreviousLargePictureKeyName);
}
