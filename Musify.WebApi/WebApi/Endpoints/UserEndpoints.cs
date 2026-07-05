using Mediator;
using Musify.Application.Users.Commands;
using Musify.Application.Users.Queries;
using WebApi.DataTransferObjects.Users;
using WebApi.Extensions;
using WebApi.Filters;

namespace WebApi.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users")
            .WithTags("Users");

        group.MapGet("/", GetUsers)
            .WithName("GetUsers")
            .WithSummary("Get Paginated Users.");

        group.MapGet("/{id}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get A User By Id.");

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create A New User.")
            .AddEndpointFilter<ValidationFilter<CreateUserRequest>>();

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

    private static async Task<IResult> CreateUser(
        IMediator mediator,
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateUserCommand(request.Id, request.Name, request.FirstName, request.SecondName),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created($"/users/{result.Value.Id}", result.Value);
    }
}
