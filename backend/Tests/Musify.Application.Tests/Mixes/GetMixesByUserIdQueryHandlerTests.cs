using Musify.Application.Mixes;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
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
        var second = TestEntities.Mix(owner.Id, MixKind.Daily, position: 1);
        var first = TestEntities.Mix(owner.Id, MixKind.Discovery, position: 0);
        var theirs = TestEntities.Mix(other.Id);
        await SeedAsync(owner, other, second, first, theirs);

        var result = await CreateHandler().Handle(new GetMixesByUserIdQuery(owner.Id), TestContext.Current.CancellationToken);
        Assert.Equal([MixKind.Discovery, MixKind.Daily], result.Select(mix => mix.Kind));
    }

    [Fact]
    public async Task Handle_NoMixes_ReturnsEmptyList()
    {
        var owner = TestEntities.User();
        await SeedAsync(owner);

        var result = await CreateHandler().Handle(new GetMixesByUserIdQuery(owner.Id), TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }
}
