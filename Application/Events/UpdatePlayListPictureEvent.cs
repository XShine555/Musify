namespace Musify.Application.Events
{
    public record UpdatePlayListPictureEvent(
        Guid PlayListId,
        string SourceBucketName,
        string SourceKeyName,
        string SmallPictureKeyName,
        int SmallPictureWidth,
        int SmallPictureHeight,
        string MediumPictureKeyName,
        int MediumPictureWidth,
        int MediumPictureHeight,
        string LargePictureKeyName,
        int LargePictureWidth,
        int LargePictureHeight);
}