using Musify.Domain.ValueObjects;

namespace Musify.Api.Models
{
    public readonly record struct GenreParameter(Genre Value)
    {
        public static bool TryParse(string? value, out GenreParameter result)
        {
            if (Enum.TryParse<Genre>(value, ignoreCase: true, out var genre) && genre != Genre.None && Enum.IsDefined(genre))
            {
                result = new GenreParameter(genre);
                return true;
            }

            result = default;
            return false;
        }
    }
}
