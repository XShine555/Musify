using Mediator;
using Musify.Api.Authentication;
using Musify.Api.Extensions;
using Musify.Api.Filters;
using Musify.Api.Models;
using Musify.Application.Shared;
using Musify.Application.Tracks;
using Musify.Application.Tracks.Responses;
using Musify.Application.Users;
using Musify.Application.Users.Responses;

namespace Musify.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users")
            .WithTags("Users");

        group.MapGet("/", GetUsers)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetUsers")
            .WithSummary("Get paginated users")
            .Produces<PaginatedResponse<UserSummaryResponse>>();

        group.MapGet("/{id:long}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get a user by id")
            .Produces<UserApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:long}/listening-history", GetListeningHistory)
            .WithName("GetListeningHistory")
            .WithSummary("Get a user's listening history")
            .Produces<IEnumerable<TrackApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:long}/listening-stats", GetListeningStats)
            .WithName("GetListeningStats")
            .WithSummary("Get a user's listening stats for the current week and streak")
            .RequireAuthorization()
            .Produces<ListeningStatsResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:long}/last-listened-track", GetLastTrackListenedByUserId)
            .WithName("GetLastTrackListenedByUserId")
            .WithSummary("Get the last track a user listened to")
            .Produces<TrackApplicationResponse>()
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/{id:long}/profile", GetUserProfile)
            .WithName("GetUserProfile")
            .WithSummary("Get a user's public profile, including follower counts")
            .Produces<UserProfileResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:long}/followers", GetFollowers)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetUserFollowers")
            .WithSummary("Get the users following a user. Visible to the user and to mutual followers")
            .Produces<PaginatedResponse<UserSummaryResponse>>()
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:long}/following", GetFollowing)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetUserFollowing")
            .WithSummary("Get the users a user follows")
            .Produces<PaginatedResponse<UserSummaryResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:long}/is-following", IsFollowing)
            .WithName("IsFollowingUser")
            .WithSummary("Check whether the current user follows another user")
            .RequireAuthorization()
            .Produces<bool>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:long}/follow", FollowUser)
            .WithName("FollowUser")
            .WithSummary("Follow a user")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:long}/follow", UnfollowUser)
            .WithName("UnfollowUser")
            .WithSummary("Unfollow a user")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetUsers(
        IMediator mediator,
        CurrentUser currentUser,
        string? usernameSearch,
        [AsParameters] PageQuery page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUsersQuery(page.PageNumber, page.PageSize, usernameSearch, currentUser.Id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUserById(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetListeningHistory(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetListeningHistoryQuery(id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetListeningStats(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken)
    {
        if (currentUser.RequiredId != id)
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden);

        var result = await mediator.Send(new GetListeningStatsQuery(id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetLastTrackListenedByUserId(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        var track = await mediator.Send(new GetLastTrackListenedByUserIdQuery(id), cancellationToken);
        return track == null ? Results.NoContent() : Results.Ok(track);
    }

    private static async Task<IResult> GetUserProfile(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserProfileQuery(id, currentUser.Id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetFollowers(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        [AsParameters] PageQuery page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserFollowersQuery(currentUser.Id, id, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetFollowing(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        [AsParameters] PageQuery page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserFollowingQuery(currentUser.Id, id, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> IsFollowing(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new IsFollowingUserQuery(currentUser.RequiredId, id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> FollowUser(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new FollowUserCommand(currentUser.RequiredId, id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UnfollowUser(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnfollowUserCommand(currentUser.RequiredId, id), cancellationToken);
        return result.ToHttpResult();
    }
}
