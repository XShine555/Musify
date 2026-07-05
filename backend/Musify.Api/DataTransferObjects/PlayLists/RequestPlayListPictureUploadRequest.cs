namespace Musify.Api.DataTransferObjects.PlayLists;

public record RequestPlayListPictureUploadRequest(
    string FileType,
    string ContentType,
    long? ExpectedSizeBytes = null);
