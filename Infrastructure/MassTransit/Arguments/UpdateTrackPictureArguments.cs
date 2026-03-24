namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record UpdateTrackPictureArguments(
        Guid TrackId,
        string OriginalPictureKeyName,
        string SmallPictureKeyName,
        string MediumPictureKeyName,
        string LargePictureKeyName);
}