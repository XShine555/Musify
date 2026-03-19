namespace Musify.Application.Events
{
    public record ResizePictureEvent(
        string BucketName,
        string KeyName,
        ResizePictureItems[] Items);
}