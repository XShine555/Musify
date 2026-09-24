namespace Musify.Application.Tracks.Responses
{
    public record TrackAudioUpload(
        Guid IntentId,
        Guid? PictureIntentId,
        string Key,
        string Name,
        string ContentType,
        string UploadUrl);

    public record TrackUploadUrlsResponse(
        Guid? PictureIntentId,
        Guid AudioIntentId,
        string Bucket,
        string? PictureKey,
        string? PictureName,
        string? PictureContentType,
        string? PictureUploadUrl,
        string AudioKey,
        string AudioName,
        string AudioContentType,
        string AudioUploadUrl,
        int ExpiresInSeconds,
        IReadOnlyList<TrackAudioUpload> Audios);
}
