namespace Musify.Api.Validators;

public static class Limits
{
    public const int TrackTitle = 200;
    public const int AlbumTitle = 200;
    public const int PlayListName = 50;
    public const int Description = 256;
    public const int EarliestReleaseYear = 1877;

    public static int LatestReleaseYear => DateTime.UtcNow.Year + 1;
}
