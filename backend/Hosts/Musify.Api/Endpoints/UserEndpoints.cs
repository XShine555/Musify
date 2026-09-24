using Mediator;
using Musify.Api.Authentication;
using Musify.Api.Extensions;
using Musify.Api.Models;
using Musify.Application.Shared;
using Musify.Application.Tracks;
using Musify.Application.Tracks.Responses;
using Musify.Application.Users;
using Musify.Application.Users.Responses;
using Musify.Api.Filters;

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
            .WithSummary("Get Paginated Users.")
            .Produces<PaginatedResponse<UserSummaryResponse>>();

        group.MapGet("/{id}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get A User By Id.")
            .Produces<UserApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/listening-history", GetListeningHistory)
            .WithName("GetListeningHistory")
            .WithSummary("Get A User'S Listening History.")
            .Produces<IEnumerable<TrackApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/listening-stats", GetListeningStats)
            .WithName("GetListeningStats")
            .WithSummary("Get A User'S Listening Stats For The Current Week And Streak.")
            .RequireAuthorization()
            .Produces<ListeningStatsResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{id}/last-listened-track", GetLastTrackListenedByUserId)
            .WithName("GetLastTrackListenedByUserId")
            .WithSummary("Get The Last Track A User Listened To.")
            .Produces<TrackApplicationResponse>()
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/{id}/profile", GetUserProfile)
            .WithName("GetUserProfile")
            .WithSummary("Get A User'S Public Profile, Including Follower Counts.")
            .Produces<UserProfileResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/followers", GetFollowers)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetUserFollowers")
            .WithSummary("Get The Users Following A User. Visible To The User And To Mutual Followers.")
            .Produces<PaginatedResponse<UserSummaryResponse>>()
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/following", GetFollowing)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetUserFollowing")
            .WithSummary("Get The Users A User Follows.")
            .Produces<PaginatedResponse<UserSummaryResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/is-following", IsFollowing)
            .WithName("IsFollowingUser")
            .WithSummary("Check Whether The Current User Follows Another User.")
            .RequireAuthorization()
            .Produces<bool>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id}/follow", FollowUser)
            .WithName("FollowUser")
            .WithSummary("Follow A User.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id}/follow", UnfollowUser)
            .WithName("UnfollowUser")
            .WithSummary("Unfollow A User.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> GetUsers(
        IMediator mediator,
        CurrentUser currentUser,
        CancellationToken cancellationToken,
        string? usernameSearch,
        [AsParameters] PageQuery page)
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

    private static async Task<IEnumerable<TrackApplicationResponse>> GetListeningHistory(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetListeningHistoryQuery(id), cancellationToken);
    }

    private static async Task<IResult> GetListeningStats(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken)
    {
        if (currentUser.RequiredId != id)
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        return Results.Ok(await mediator.Send(new GetListeningStatsQuery(id), cancellationToken));
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
        CancellationToken cancellationToken,
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetUserFollowersQuery(currentUser.Id, id, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetFollowing(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken,
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetUserFollowingQuery(currentUser.Id, id, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<bool> IsFollowing(
        IMediator mediator,
        CurrentUser currentUser,
        long id,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new IsFollowingUserQuery(currentUser.RequiredId, id), cancellationToken);
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
