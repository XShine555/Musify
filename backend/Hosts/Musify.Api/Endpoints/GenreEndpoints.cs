using Mediator;
using Musify.Application.Genres;
using Musify.Application.Genres.Responses;

namespace Musify.Api.Endpoints;

public static class GenreEndpoints
{
    public static IEndpointRouteBuilder MapGenreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/genres")
            .WithTags("Genres");

        group.MapGet("/", GetGenres)
            .WithName("GetGenres")
            .WithSummary("Get The Genres That Have At Least One Track, Most Populated First. Works Anonymously.")
            .Produces<IReadOnlyList<GenreResponse>>();

        group.MapGet("/available", GetAvailableGenres)
            .WithName("GetAvailableGenres")
            .WithSummary("Get Every Genre A Track Can Be Tagged With, Including The Genres It Cannot Be Combined With. Works Anonymously.")
            .Produces<IReadOnlyList<AvailableGenreResponse>>();

        return app;
    }

    private static async Task<IReadOnlyList<AvailableGenreResponse>> GetAvailableGenres(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetAvailableGenresQuery(), cancellationToken);
    }

    private static async Task<IReadOnlyList<GenreResponse>> GetGenres(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetGenresQuery(), cancellationToken);
    }
}
