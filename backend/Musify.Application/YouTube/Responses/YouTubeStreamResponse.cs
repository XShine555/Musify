namespace Musify.Application.YouTube.Responses
{
    public enum YouTubeStreamMode
    {
        Server = 0,
        YouTube = 1
    }

    public record YouTubeStreamResponse(
        YouTubeStreamMode Mode,
        string StreamUrl,
        string Ticket,
        int ExpiresInSeconds,
        Guid TrackId);
}
