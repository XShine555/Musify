using Mediator;
using Musify.Application.Configuration.Responses;

namespace Musify.Application.Configuration;


public record GetPlaybackPublicConfigQuery : IQuery<PlaybackPublicConfigResponse>;

public class GetPlaybackPublicConfigQueryHandler(PlaybackConfiguration playbackConfiguration)
    : IQueryHandler<GetPlaybackPublicConfigQuery, PlaybackPublicConfigResponse>
{
    public ValueTask<PlaybackPublicConfigResponse> Handle(GetPlaybackPublicConfigQuery query, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new PlaybackPublicConfigResponse(
            playbackConfiguration.AllowAnonymousListening,
            playbackConfiguration.AnonymousFragmentSeconds));
}
