namespace Musify.Infrastructure.MassTransit.Activities.Tracks
{
    internal record DeleteTrackArguments(
        Guid TrackId,
        Guid UserId);
}
