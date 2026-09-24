namespace Musify.Application.Events;

public record TrackProcessingFailed(
    Guid TrackId,
    string? Bucket,
    string? PictureKey,
    string? AudioKey);
