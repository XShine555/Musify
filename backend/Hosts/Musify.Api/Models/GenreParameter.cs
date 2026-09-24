using Musify.Domain.ValueObjects;

namespace Musify.Api.Models;

/// <summary>Query-string binding for a genre name (case-insensitive); unknown names are rejected with a 400.</summary>
public readonly record struct GenreParameter(Genre Value)
{
    public static bool TryParse(string? value, out GenreParameter result)
    {
        if (Enum.TryParse<Genre>(value, ignoreCase: true, out var genre) && Enum.IsDefined(genre))
        {
            result = new GenreParameter(genre);
            return true;
        }

        result = default;
        return false;
    }
}
