namespace Musify.Application.Events
{
    public record ImageSize(
        string SavePath,
        int Width,
        int Height);
}