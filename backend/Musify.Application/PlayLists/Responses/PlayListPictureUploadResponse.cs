namespace Musify.Application.PlayLists.Responses
{
    public record PlayListPictureUploadResponse(
        Guid IntentId,
        string Bucket,
        string Key,
        string PictureName,
        string ContentType,
        int ExpiresInSeconds,
        string UploadUrl);
}
