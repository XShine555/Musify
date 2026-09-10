namespace Musify.Application.Albums.Responses
{
    public record AlbumPictureUploadResponse(
        Guid IntentId,
        string Bucket,
        string Key,
        string PictureName,
        string ContentType,
        int ExpiresInSeconds,
        string UploadUrl);
}
