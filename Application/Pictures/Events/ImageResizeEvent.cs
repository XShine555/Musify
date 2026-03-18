namespace Musify.Application.Pictures.Events
{
    public record ImageResizeEvent(
        Guid UploadId,
        int Width,
        int Height,
        string SaveRoute);
}