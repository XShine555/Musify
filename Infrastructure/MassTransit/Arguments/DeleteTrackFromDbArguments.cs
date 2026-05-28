namespace Musify.Infrastructure.MassTransit.Activities.Tracks
{
    internal record DeleteTrackFromDbArguments(
        Guid TrackId);
}
