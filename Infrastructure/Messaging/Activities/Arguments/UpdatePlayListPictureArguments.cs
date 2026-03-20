namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record UpdatePlayListPictureArguments(
        Guid PlayListId,
        string SmallPictureKeyName,
        string MediumPictureKeyName,
        string LargePictureKeyName);
}