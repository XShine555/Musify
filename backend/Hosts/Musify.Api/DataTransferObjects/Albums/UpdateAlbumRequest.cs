namespace Musify.Api.DataTransferObjects.Albums;

public record UpdateAlbumRequest(
    string Title,
    string? Description,
    int? ReleaseYear,
    Guid? PictureIntentId);
