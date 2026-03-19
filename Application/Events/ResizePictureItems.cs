namespace Musify.Application.Events
{
    public record ResizePictureItems(
        int Width,
        int Height,
        string SaveOnRoute);
}