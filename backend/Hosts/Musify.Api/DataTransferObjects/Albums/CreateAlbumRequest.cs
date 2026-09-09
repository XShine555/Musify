namespace Musify.Api.DataTransferObjects.Albums;

public record CreateAlbumRequest(
    string Title,
    string? Description,
    int? ReleaseYear);
