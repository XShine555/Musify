namespace Musify.Infrastructure.MassTransit.Arguments
{
    internal record DeleteTrackFromDbArguments(
        Guid TrackId);
}
