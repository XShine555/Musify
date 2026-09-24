namespace Musify.Application.Shared;

public record ImageSize(
    string SavePath,
    int Width,
    int Height);

public record ImageSizes(
    ImageSize Small,
    ImageSize Medium,
    ImageSize Large);
