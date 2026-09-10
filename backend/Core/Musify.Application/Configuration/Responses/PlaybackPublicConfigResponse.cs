namespace Musify.Application.Configuration.Responses
{
    /// <param name="AllowAnonymousListening">Whether listeners without a session can stream music at all.</param>
    /// <param name="AnonymousFragmentSeconds">How many seconds of a track an anonymous listener may hear.
    /// Zero means no cap. Meaningless when <paramref name="AllowAnonymousListening"/> is false.</param>
    public record PlaybackPublicConfigResponse(bool AllowAnonymousListening, int AnonymousFragmentSeconds);
}
