using Musify.Domain.ValueObjects;

namespace Musify.Application.Genres.Responses
{
    public record AvailableGenreResponse(Genre Genre, IReadOnlyList<Genre> IncompatibleWith);
}
