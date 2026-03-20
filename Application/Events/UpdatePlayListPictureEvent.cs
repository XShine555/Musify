namespace Musify.Application.Events
{
    public record UpdatePlayListPictureEvent(
        Guid PlayListId,
        string OriginalPictureKeyName);
}