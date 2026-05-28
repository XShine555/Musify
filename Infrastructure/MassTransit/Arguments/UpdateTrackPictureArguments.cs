namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record UpdateTrackPictureArguments(
        Guid TrackId,
        string OriginalPictureKey,
        string SmallPictureVariable,
        string MediumPictureVariable,
        string LargePictureVariable);
}