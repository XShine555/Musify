namespace Musify.Api.DataTransferObjects.Tracks
{
    public record AudioUploadRequest(
        string FileType,
        string ContentType,
        long? ExpectedSizeBytes = null);

    public record RequestTrackUploadUrlsRequest(
        string? PictureFileType = null,
        string? PictureContentType = null,
        string? AudioFileType = null,
        string? AudioContentType = null,
        long? ExpectedPictureSizeBytes = null,
        long? ExpectedAudioSizeBytes = null,
        IReadOnlyList<AudioUploadRequest>? Audios = null);
}
