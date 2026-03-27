namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record UpdateTrackPictureArguments(
        Guid TrackId,
        string OriginalPictureKeyName,
        string SmallPictureVariableName,
        string MediumPictureVariableName,
        string LargePictureVariableName);
}