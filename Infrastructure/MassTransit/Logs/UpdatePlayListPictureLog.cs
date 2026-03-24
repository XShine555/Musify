namespace Musify.Infrastructure.Messaging.Activities.Logs
{
    public record UpdatePlayListPictureLog(
        Guid PlayListId,
        string? PreviousSmallPictureKeyName,
        string? PreviousMediumPictureKeyName,
        string? PreviousLargePictureKeyName);
}
