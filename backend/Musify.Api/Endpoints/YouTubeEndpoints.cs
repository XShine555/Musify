using Mediator;
using Musify.Api.Authentication;
using Musify.Api.Extensions;
using Musify.Application.Contracts;
using Musify.Application.YouTube;
using Musify.Application.YouTube.Responses;

namespace Musify.Api.Endpoints;

public static class YouTubeEndpoints
{
    public static IEndpointRouteBuilder MapYouTubeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/youtube")
            .WithTags("YouTube");

        group.MapGet("/search", SearchTracks)
            .WithName("SearchYouTubeTracks")
            .WithSummary("Search Songs On YouTube Music.")
            .RequireAuthorization()
            .Produces<YouTubeSearchResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapGet("/tracks/{videoId}/stream", GetTrackStream)
            .WithName("GetYouTubeTrackStream")
            .WithSummary("Resolve Playback For A YouTube Track: Server Stream If Downloaded, Direct YouTube Stream Otherwise.")
            .RequireAuthorization()
            .Produces<YouTubeStreamResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetTrackStream(
        IMediator mediator,
        CurrentUser currentUser,
        string videoId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetYouTubeTrackStreamQuery(videoId, currentUser.RequiredId),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> SearchTracks(
        IMediator mediator,
        CancellationToken cancellationToken,
        string query,
        string? continuation = null)
    {
        var result = await mediator.Send(
            new SearchYouTubeTracksQuery(query, continuation ?? string.Empty),
            cancellationToken);

        return result.ToHttpResult();
    }
}
