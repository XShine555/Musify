using Musify.Application.Genres;
using Musify.Application.Genres.Responses;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Genres;

public sealed class GetAvailableGenresQueryHandlerTests
{
    private static async Task<IReadOnlyList<AvailableGenreResponse>> RunAsync() =>
        await new GetAvailableGenresQueryHandler().Handle(new GetAvailableGenresQuery(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_ReturnsEveryGenre()
    {
        var result = await RunAsync();

        Assert.Equal(Enum.GetValues<Genre>(), result.Select(r => r.Genre));
    }

    [Fact]
    public async Task Handle_ListsIncompatibleGenresSymmetrically()
    {
        var result = await RunAsync();

        var metal = result.Single(r => r.Genre == Genre.Metal);
        var lofi = result.Single(r => r.Genre == Genre.Lofi);
        Assert.Contains(Genre.Lofi, metal.IncompatibleWith);
        Assert.Contains(Genre.Metal, lofi.IncompatibleWith);
        Assert.Empty(result.Single(r => r.Genre == Genre.Pop).IncompatibleWith);
    }
}
