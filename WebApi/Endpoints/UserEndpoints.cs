using Mediator;
using Musify.Application.Users.Commands;
using Musify.Application.Users.Queries;
using WebApi.DataTransferObjects.Users;
using WebApi.Extensions;

namespace WebApi.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users");

        group.MapGet("/", GetUsers)
            .WithName("GetUsers")
            .WithSummary("Get paginated users.");

        group.MapGet("/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get a user by ID.");

        group.MapGet("/keycloak/{id:guid}", GetUserByKeycloak)
            .WithName("GetUserByKeycloak")
            .WithSummary("Get a user by Keycloak ID.");

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create a new user.");

        return app;
    }

    private static async Task<IResult> GetUsers(
        IMediator mediator,
        int pageNumber = 1,
        int pageSize = 10,
        string? usernameSearch = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsersQuery(pageNumber, pageSize, usernameSearch), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUserById(
        IMediator mediator,
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUserByKeycloak(
        IMediator mediator,
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserByKeycloakQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateUser(
        IMediator mediator,
        CreateUserRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateUserCommand(request.Id, request.Name, request.FirstName, request.SecondName),
            ct);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created($"/api/users/{result.Value.Id}", result.Value);
    }
}
