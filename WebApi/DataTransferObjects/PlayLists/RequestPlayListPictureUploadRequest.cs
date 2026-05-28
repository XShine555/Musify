namespace WebApi.DataTransferObjects.PlayLists;

public record RequestPlayListPictureUploadRequest(
    string FileType,
    string ContentType,
    long? ExpectedSizeBytes = null);
