namespace Musify.Application.Events
{
    public record UpdatePlayListPictureEvent(
        Guid PlayListId,
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