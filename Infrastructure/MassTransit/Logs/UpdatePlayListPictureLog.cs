namespace Musify.Infrastructure.MassTransit.Activities.Logs
{
    public record UpdatePlayListPictureLog(
        Guid PlayListId,
        string PreviousOriginalPictureKeyName,
        string PreviousSmallPictureKeyName,
        string PreviousMediumPictureKeyName,
        string PreviousLargePictureKeyName);
}
