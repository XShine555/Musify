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
            .WithSummary("Get the public playback configuration (whether anonymous listening is allowed). Works anonymously")
            .Produces<PlaybackPublicConfigResponse>();

        return app;
    }

    private static async Task<IResult> GetPlaybackPublicConfig(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPlaybackPublicConfigQuery(), cancellationToken);
        return Results.Ok(result);
    }
}
