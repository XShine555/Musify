namespace Musify.Application.PlayLists.Responses
{
    public record PlayListPictureUploadResponse(
        string Bucket,
        string Key,
        string PictureName,
        string ContentType,
        int ExpiresInSeconds,
        string UploadUrl);
}
