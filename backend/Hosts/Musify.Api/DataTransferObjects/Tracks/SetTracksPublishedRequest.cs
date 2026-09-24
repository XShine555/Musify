namespace Musify.Api.DataTransferObjects.Tracks
{
    public record SetTracksPublishedRequest(IReadOnlyList<Guid> TrackIds, bool IsPublished);
}
