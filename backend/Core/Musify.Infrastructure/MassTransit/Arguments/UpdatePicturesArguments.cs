namespace Musify.Infrastructure.MassTransit.Arguments;

public record UpdatePicturesArguments(
    Guid SubjectId,
    string OriginalPictureKey,
    string SmallPictureVariable,
    string MediumPictureVariable,
    string LargePictureVariable);
