namespace Musify.Infrastructure.MassTransit.Logs
{
    public record UpdatePlayListPictureLog(
        Guid PlayListId,
        string PreviousOriginalPictureKey,
        string PreviousSmallPictureKey,
        string PreviousMediumPictureKey,
        string PreviousLargePictureKey);
}
