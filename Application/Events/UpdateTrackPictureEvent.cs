namespace Musify.Application.Events
{
    public record UpdateTrackPictureEvent(
        Guid TrackId,
        string SourceBucketName,
        string SourceKeyName,
        string SmallPictureRoute,
        int SmallPictureWidth,
        int SmallPictureHeight,
        string MediumPictureRoute,
        int MediumPictureWidth,
        int MediumPictureHeight,
        string LargePictureRoute,
        int LargePictureWidth,
        int LargePictureHeight);
}