using Mediator;
using Musify.Api.Authentication;
using Musify.Api.Extensions;
using Musify.Application.Mixes;
using Musify.Application.Mixes.Responses;

namespace Musify.Api.Endpoints;

public static class MixEndpoints
{
    public static IEndpointRouteBuilder MapMixEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/mixes")
            .WithTags("Mixes")
            .RequireAuthorization();

        group.MapGet("/", GetMixes)
            .WithName("GetMixes")
            .WithSummary("Get the server-generated mixes of the current user")
            .Produces<IReadOnlyList<MixApplicationResponse>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetMixById)
            .WithName("GetMixById")
            .WithSummary("Get a mix with all of its songs")
            .Produces<MixApplicationResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetMixes(
        IMediator mediator,
        CurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMixesByUserIdQuery(currentUser.RequiredId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMixById(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMixByIdQuery(currentUser.RequiredId, id), cancellationToken);
        return result.ToHttpResult();
    }
}
