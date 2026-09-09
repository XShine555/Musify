namespace Musify.Api.Validators.Albums;

public static class AlbumReleaseYear
{
    public const int Earliest = 1877;

    public static int Latest => DateTime.UtcNow.Year + 1;
}
