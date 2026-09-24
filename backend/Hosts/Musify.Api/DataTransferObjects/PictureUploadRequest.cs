namespace Musify.Api.DataTransferObjects;

public record PictureUploadRequest(
    string FileType,
    string ContentType,
    long? ExpectedSizeBytes = null);
