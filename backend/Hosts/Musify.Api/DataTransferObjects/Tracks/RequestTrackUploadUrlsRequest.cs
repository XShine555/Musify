namespace Musify.Api.DataTransferObjects.Tracks;

public record RequestTrackUploadUrlsRequest(
    string PictureFileType,
    string PictureContentType,
    string AudioFileType,
    string AudioContentType,
    long? ExpectedPictureSizeBytes = null,
    long? ExpectedAudioSizeBytes = null);
