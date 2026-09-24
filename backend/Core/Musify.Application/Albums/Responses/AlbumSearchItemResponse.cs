namespace Musify.Application.Albums.Responses;

public record AlbumSearchItemResponse(AlbumApplicationResponse Album)
{
    public static AlbumSearchItemResponse FromAlbum(AlbumApplicationResponse album) => new(album);
}
