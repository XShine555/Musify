namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record UpdateTrackPictureArguments(
        Guid TrackId,
        string SmallPictureKeyName,
        string MediumPictureKeyName,
        string LargePictureKeyName);
}