namespace Musify.Application.Events;

public record DeleteTrackEvent(
    Guid TrackId,
    long UserId);
