using Mediator;
using Musify.Application.Configuration;
using Musify.Application.Configuration.Responses;

namespace Musify.Api.Endpoints;

public static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/config")
            .WithTags("Config");

        group.MapGet("/playback", GetPlaybackPublicConfig)
            .WithName("GetPlaybackPublicConfig")
            .WithSummary("Get The Public Playback Configuration (Whether Anonymous Listening Is Allowed). Works Anonymously.")
            .Produces<PlaybackPublicConfigResponse>();

        return app;
    }

    private static async Task<PlaybackPublicConfigResponse> GetPlaybackPublicConfig(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetPlaybackPublicConfigQuery(), cancellationToken);
    }
}
