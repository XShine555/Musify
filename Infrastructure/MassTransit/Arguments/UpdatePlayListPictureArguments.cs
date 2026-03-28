namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record UpdatePlayListPictureArguments(
        Guid PlayListId,
        string OriginalPictureKey,
        string SmallPictureVariable,
        string MediumPictureVariable,
        string LargePictureVariable);
}