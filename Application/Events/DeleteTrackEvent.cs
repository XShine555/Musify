namespace Musify.Application.Events
{
    public record DeleteTrackEvent(
        Guid TrackId,
        Guid UserId);
}
