using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.Users;
using Musify.Api.Extensions;
using Musify.Api.Filters;
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
            .WithName("GetUsers")
            .WithSummary("Get Paginated Users.")
            .Produces<PaginatedResponse<UserApplicationResponse>>();

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
            .Produces<ListeningStatsResponse>();

        group.MapGet("/{id}/last-listened-track", GetLastTrackListenedByUserId)
            .WithName("GetLastTrackListenedByUserId")
            .WithSummary("Get The Last Track A User Listened To.")
            .Produces<TrackApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/profile", GetUserProfile)
            .WithName("GetUserProfile")
            .WithSummary("Get A User'S Public Profile, Including Follower Counts.")
            .Produces<UserProfileResponse>()
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

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create A New User.")
            .AddEndpointFilter<ValidationFilter<CreateUserRequest>>()
            .Produces<UserApplicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetUsers(
        IMediator mediator,
        CancellationToken cancellationToken,
        string? usernameSearch,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetUsersQuery(pageNumber, pageSize, usernameSearch), cancellationToken);
        return result.ToHttpResult();
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

    private static async Task<ListeningStatsResponse> GetListeningStats(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetListeningStatsQuery(id), cancellationToken);
    }

    private static async Task<IResult> GetLastTrackListenedByUserId(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLastTrackListenedByUserIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUserProfile(
        IMediator mediator,
        long id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserProfileQuery(id), cancellationToken);
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

    private static async Task<IResult> CreateUser(
        IMediator mediator,
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateUserCommand(request.Id, request.Name, request.FirstName, request.SecondName),
            cancellationToken);

        return result.ToCreatedResult(user => $"/users/{user.Id}");
    }
}
