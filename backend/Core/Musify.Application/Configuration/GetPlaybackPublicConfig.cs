using Mediator;
using Musify.Application.Configuration.Responses;

namespace Musify.Application.Configuration
{
    /// <summary>Reads the parts of <see cref="PlaybackConfiguration"/> that are safe to expose to an
    /// unauthenticated client, so it can decide whether to gate the app behind sign-in at all.</summary>
    public record GetPlaybackPublicConfigQuery : IQuery<PlaybackPublicConfigResponse>;

    public class GetPlaybackPublicConfigQueryHandler(PlaybackConfiguration playbackConfiguration)
        : IQueryHandler<GetPlaybackPublicConfigQuery, PlaybackPublicConfigResponse>
    {
        public ValueTask<PlaybackPublicConfigResponse> Handle(GetPlaybackPublicConfigQuery query, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new PlaybackPublicConfigResponse(
                playbackConfiguration.AllowAnonymousListening,
                playbackConfiguration.AnonymousFragmentSeconds));
    }
}
