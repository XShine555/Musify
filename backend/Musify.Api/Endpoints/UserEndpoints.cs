using Mediator;
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
