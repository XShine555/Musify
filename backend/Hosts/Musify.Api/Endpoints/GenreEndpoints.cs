using Mediator;
using Musify.Application.Genres;
using Musify.Application.Genres.Responses;

namespace Musify.Api.Endpoints
{
    public static class GenreEndpoints
    {
        public static IEndpointRouteBuilder MapGenreEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/genres")
                .WithTags("Genres");

            group.MapGet("/", GetGenres)
                .WithName("GetGenres")
                .WithSummary("Get the genres that have at least one track, most populated first. Works anonymously")
                .Produces<IReadOnlyList<GenreResponse>>();

            group.MapGet("/available", GetAvailableGenres)
                .WithName("GetAvailableGenres")
                .WithSummary("Get every genre a track can be tagged with, including the genres it cannot be combined with. Works anonymously")
                .Produces<IReadOnlyList<AvailableGenreResponse>>();

            return app;
        }

        private static async Task<IResult> GetAvailableGenres(
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetAvailableGenresQuery(), cancellationToken);
            return Results.Ok(result);
        }

        private static async Task<IResult> GetGenres(
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetGenresQuery(), cancellationToken);
            return Results.Ok(result);
        }
    }
}
