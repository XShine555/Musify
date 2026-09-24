using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class DeletePlayListCommandHandlerTests : HandlerTestBase
{
    private readonly IEventBus eventBus = Substitute.For<IEventBus>();

    private DeletePlayListCommandHandler CreateHandler() => new(Database, eventBus, NoOpLogger<DeletePlayListCommandHandler>());

    [Fact]
    public async Task Handle_Owner_MarksPlayListAsRemoving()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var result = await CreateHandler().Handle(new DeletePlayListCommand(owner.Id, playList.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var stored = await Database.PlayLists.FindAsync([playList.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal(LifeCycleStatus.Removing, stored.LifeCycleStatus);
        await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.DeletePlayListEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new DeletePlayListCommand(1, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var result = await CreateHandler().Handle(new DeletePlayListCommand(stranger.Id, playList.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }
}
