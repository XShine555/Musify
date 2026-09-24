namespace Musify.Infrastructure.MassTransit.Arguments;

public record UpdatePlayListPictureArguments(
    Guid PlayListId,
    string OriginalPictureKey,
    string SmallPictureVariable,
    string MediumPictureVariable,
    string LargePictureVariable);
