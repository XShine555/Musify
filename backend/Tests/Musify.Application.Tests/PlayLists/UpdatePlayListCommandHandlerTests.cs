using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.PlayLists;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class UpdatePlayListCommandHandlerTests : HandlerTestBase
{
    private readonly IEventBus eventBus = Substitute.For<IEventBus>();
    private readonly IStorageService storageService = Substitute.For<IStorageService>();

    private UpdatePlayListCommandHandler CreateHandler() => new(
        eventBus,
        Database,
        new UploadIntentValidator(Database, storageService),
        NoOpLogger<UpdatePlayListCommandHandler>(),
        TestConfigurations.Storage(),
        TestConfigurations.PlayList(),
        TestConfigurations.UploadIntent());

    [Fact]
    public async Task Handle_Owner_RenamesPlayList()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id, "Old Name");
        await SeedAsync(owner, playList);

        var command = new UpdatePlayListCommand(owner.Id, playList.Id, "New Name", null, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("New Name", result.Value.Name);
    }

    [Fact]
    public async Task Handle_PlayListMissing_ReturnsNotFound()
    {
        var command = new UpdatePlayListCommand(1, Guid.NewGuid(), "New Name", null, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var command = new UpdatePlayListCommand(stranger.Id, playList.Id, "Hijacked", null, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_BlankNameAndDescription_LeavesThemUnchanged()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id, "Original Name", "Original description");
        await SeedAsync(owner, playList);

        var command = new UpdatePlayListCommand(owner.Id, playList.Id, "   ", "   ", null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("Original Name", result.Value.Name);
        Assert.Equal("Original description", result.Value.Description);
    }
}
