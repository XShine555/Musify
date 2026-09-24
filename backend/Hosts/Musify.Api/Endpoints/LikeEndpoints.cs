using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.Likes;
using Musify.Api.Extensions;
using Musify.Application.Likes;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

namespace Musify.Api.Endpoints;

public static class LikeEndpoints
{
    public static IEndpointRouteBuilder MapLikeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/likes")
            .WithTags("Likes")
            .RequireAuthorization();

        group.MapGet("/", GetLikedTracks)
            .WithName("GetLikedTracks")
            .WithSummary("Get The Current User'S Liked Tracks.")
            .Produces<PaginatedResponse<TrackApplicationResponse>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/toggle", ToggleTrackLike)
            .WithName("ToggleTrackLike")
            .WithSummary("Like Or Unlike A Track By Id.")
            .Produces<bool>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetLikedTracks(
        IMediator mediator,
        CurrentUser currentUser,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetLikedTracksQuery(currentUser.RequiredId, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ToggleTrackLike(
        IMediator mediator,
        CurrentUser currentUser,
        ToggleLikeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ToggleTrackLikeCommand(currentUser.RequiredId, request.TrackId),
            cancellationToken);

        return result.ToHttpResult();
    }
}
