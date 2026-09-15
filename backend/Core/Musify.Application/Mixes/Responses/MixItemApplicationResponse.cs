namespace Musify.Application.Mixes.Responses
{
    public record MixItemApplicationResponse(
        Guid TrackId,
        string Title,
        string? Artist,
        double DurationSeconds);
}
