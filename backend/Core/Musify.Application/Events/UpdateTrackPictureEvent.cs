using Musify.Application.Shared;

namespace Musify.Application.Events;

public record UpdateTrackPictureEvent(
    Guid TrackId,
    string Bucket,
    string SourceKey,
    ImageSizes Sizes);
