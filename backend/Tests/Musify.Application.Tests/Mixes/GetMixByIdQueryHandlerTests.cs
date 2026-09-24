using ErrorOr;
using Musify.Application.Mixes;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Mixes;

public sealed class GetMixByIdQueryHandlerTests : HandlerTestBase
{
    private GetMixByIdQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_OwnedMix_ReturnsItWithOrderedItems()
    {
        var owner = TestEntities.User();
        var firstTrack = TestEntities.Track(owner, "First");
        var secondTrack = TestEntities.Track(owner, "Second");
        var mix = TestEntities.Mix(owner.Id, MixKind.Daily);
        var second = TestEntities.MixItem(mix.Id, secondTrack.Id, position: 1);
        var first = TestEntities.MixItem(mix.Id, firstTrack.Id, position: 0);
        await SeedAsync(owner, firstTrack, secondTrack, mix, second, first);

        var result = await CreateHandler().Handle(new GetMixByIdQuery(owner.Id, mix.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(MixKind.Daily, result.Value.Kind);
        Assert.Equal(["First", "Second"], result.Value.Items.Select(item => item.Title));
    }

    [Fact]
    public async Task Handle_MixMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new GetMixByIdQuery(1, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_MixBelongsToAnotherUser_ReturnsNotFound()
    {
        var owner = TestEntities.User(1, "owner");
        var mix = TestEntities.Mix(owner.Id);
        await SeedAsync(owner, mix);

        var result = await CreateHandler().Handle(new GetMixByIdQuery(2, mix.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
