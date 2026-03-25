namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record UpdatePlayListPictureArguments(
        Guid PlayListId,
        string OriginalPictureKeyName,
        string SmallPictureKeyName,
        string MediumPictureKeyName,
        string LargePictureKeyName);
}