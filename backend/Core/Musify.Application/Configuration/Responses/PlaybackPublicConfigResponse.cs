namespace Musify.Application.Configuration.Responses
{

    public record PlaybackPublicConfigResponse(bool AllowAnonymousListening, int AnonymousFragmentSeconds);
}
