using Musify.Domain.ValueObjects;

namespace Musify.Application.Genres.Responses
{
    public record GenreResponse(Genre Genre, int TrackCount);
}
