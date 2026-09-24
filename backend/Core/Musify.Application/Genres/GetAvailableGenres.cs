using Mediator;
using Musify.Domain.ValueObjects;
using Musify.Application.Genres.Responses;

namespace Musify.Application.Genres;

public record GetAvailableGenresQuery : IQuery<IReadOnlyList<AvailableGenreResponse>>;

public class GetAvailableGenresQueryHandler
    : IQueryHandler<GetAvailableGenresQuery, IReadOnlyList<AvailableGenreResponse>>
{
    public ValueTask<IReadOnlyList<AvailableGenreResponse>> Handle(GetAvailableGenresQuery request, CancellationToken cancellationToken)
    {
        var all = Enum.GetValues<Genre>();

        IReadOnlyList<AvailableGenreResponse> result = all
            .Select(genre => new AvailableGenreResponse(
                genre,
                all.Where(other => !GenreCompatibility.AreCompatible(genre, other)).ToList()))
            .ToList();

        return ValueTask.FromResult(result);
    }
}
