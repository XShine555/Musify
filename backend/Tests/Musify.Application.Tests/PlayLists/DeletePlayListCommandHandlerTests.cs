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

        var result = await CreateHandler().Handle(new DeletePlayListCommand(owner.Id, playList.Id), CancellationToken.None);

        Assert.False(result.IsError);
        var stored = await Database.PlayLists.FindAsync(playList.Id);
        Assert.NotNull(stored);
        Assert.Equal(LifeCycleStatus.Removing, stored.LifeCycleStatus);
        await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.DeletePlayListEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new DeletePlayListCommand(1, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var result = await CreateHandler().Handle(new DeletePlayListCommand(stranger.Id, playList.Id), CancellationToken.None);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }
}
