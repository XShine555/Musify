namespace Musify.Application.Tracks.Responses
{
    public record TrackUploadUrlsResponse(
        string Bucket,
        string PictureKey,
        string PictureName,
        string PictureContentType,
        string PictureUploadUrl,
        string AudioKey,
        string AudioName,
        string AudioContentType,
        string AudioUploadUrl,
        int ExpiresInSeconds);
}
