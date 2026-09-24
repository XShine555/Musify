namespace Musify.Api.DataTransferObjects.Tracks
{
    public record DeleteTracksRequest(IReadOnlyList<Guid> TrackIds);
}
