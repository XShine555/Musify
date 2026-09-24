namespace Musify.Api.DataTransferObjects
{
    public record AddTracksRequest(IReadOnlyList<Guid> TrackIds);
}
