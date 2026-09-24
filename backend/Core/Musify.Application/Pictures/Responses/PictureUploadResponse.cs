namespace Musify.Application.Pictures.Responses
{
    public record PictureUploadResponse(
        Guid IntentId,
        string Bucket,
        string Key,
        string PictureName,
        string ContentType,
        int ExpiresInSeconds,
        string UploadUrl);
}
