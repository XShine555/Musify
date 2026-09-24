namespace Musify.Application.Tracks.Responses
{
    public record SetTracksPublishedResponse(
        IReadOnlyList<Guid> UpdatedTrackIds,
        IReadOnlyList<Guid> SkippedTrackIds);
}
