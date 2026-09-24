namespace Musify.Application.Tracks.Responses
{
    public record DeleteTracksResponse(
        IReadOnlyList<Guid> DeletedTrackIds,
        IReadOnlyList<Guid> FailedTrackIds);
}
