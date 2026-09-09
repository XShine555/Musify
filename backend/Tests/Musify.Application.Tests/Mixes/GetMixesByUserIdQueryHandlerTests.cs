using Musify.Application.Mixes;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Mixes;

public sealed class GetMixesByUserIdQueryHandlerTests : HandlerTestBase
{
    private GetMixesByUserIdQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_ReturnsOnlyThatUsersMixesOrderedByPosition()
    {
        var owner = TestEntities.User(1, "owner");
        var other = TestEntities.User(2, "other");
        var second = TestEntities.Mix(owner.Id, "Second", position: 1);
        var first = TestEntities.Mix(owner.Id, "First", position: 0);
        var theirs = TestEntities.Mix(other.Id, "Theirs");
        await SeedAsync(owner, other, second, first, theirs);

        var result = await CreateHandler().Handle(new GetMixesByUserIdQuery(owner.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(["First", "Second"], result.Value.Select(mix => mix.Title));
    }

    [Fact]
    public async Task Handle_NoMixes_ReturnsEmptyList()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner);

        var result = await CreateHandler().Handle(new GetMixesByUserIdQuery(owner.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }
}
