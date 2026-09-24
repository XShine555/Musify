namespace Musify.Application.Tracks.Responses
{
    public record TrackStatusCountsResponse(
        int Total,
        int Pending,
        int Processing,
        int Completed,
        int Failed,
        int Published,
        int Unpublished);
}
