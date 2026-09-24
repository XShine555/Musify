namespace Musify.Application.Tracks.Responses
{
    public record TrackStreamResponse(
        string ManifestUrl,
        string Ticket,
        int ExpiresInSeconds,
        Guid? ListenId = null);
}
