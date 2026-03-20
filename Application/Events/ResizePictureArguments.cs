namespace Musify.Application.Events
{
    public record ResizePictureArguments(
        int Width,
        int Height,
        string SaveOnRoute);
}