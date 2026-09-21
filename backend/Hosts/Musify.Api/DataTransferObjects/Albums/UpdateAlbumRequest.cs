namespace Musify.Api.DataTransferObjects.Albums;

public record UpdateAlbumRequest(
    string NewTitle,
    string? NewDescription,
    int? NewReleaseYear,
    Guid? NewPictureIntentId);
