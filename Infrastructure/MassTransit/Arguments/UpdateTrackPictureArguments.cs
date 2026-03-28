namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record UpdateTrackPictureArguments(
        Guid TrackId,
        string OriginalPictureKey,
        string SmallPictureVariable,
        string MediumPictureVariable,
        string LargePictureVariable);
}