using Musify.Application.Genres;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Genres;

public sealed class GetGenresQueryHandlerTests : HandlerTestBase
{
    private GetGenresQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_NoTracks_ReturnsEmpty()
    {
        var result = await CreateHandler().Handle(new GetGenresQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyGenresInUse_MostPopulatedFirst()
    {
        var owner = TestEntities.User();
        var rockA = TestEntities.Track(owner, "Rock A", tags: [Genre.Rock]);
        var rockB = TestEntities.Track(owner, "Rock B", tags: [Genre.Rock, Genre.Indie]);
        var jazz = TestEntities.Track(owner, "Jazz", tags: [Genre.Jazz]);
        await SeedAsync(
            owner, rockA, rockB, jazz);

        var result = await CreateHandler().Handle(new GetGenresQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(
            [(Genre.Rock, 2), (Genre.Jazz, 1), (Genre.Indie, 1)],
            result.Select(r => (r.Genre, r.TrackCount)));
    }

}
