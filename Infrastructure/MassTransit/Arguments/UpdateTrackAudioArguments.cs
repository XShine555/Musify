namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record UpdateTrackAudioArguments(
        Guid TrackId,
        string AudioFolderKeyName);
}