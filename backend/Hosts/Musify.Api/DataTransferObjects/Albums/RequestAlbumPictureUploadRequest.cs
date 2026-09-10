namespace Musify.Api.DataTransferObjects.Albums;

public record RequestAlbumPictureUploadRequest(
    string FileType,
    string ContentType,
    long? ExpectedSizeBytes = null);
