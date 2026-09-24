using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.Likes;
using Musify.Api.Extensions;
using Musify.Application.Likes;
using Musify.Api.Models;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Api.Filters;

namespace Musify.Api.Endpoints;

public static class LikeEndpoints
{
    public static IEndpointRouteBuilder MapLikeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/likes")
            .WithTags("Likes")
            .RequireAuthorization();

        group.MapGet("/", GetLikedTracks)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
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
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetLikedTracksQuery(currentUser.RequiredId, page.PageNumber, page.PageSize), cancellationToken);
        return Results.Ok(result);
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
