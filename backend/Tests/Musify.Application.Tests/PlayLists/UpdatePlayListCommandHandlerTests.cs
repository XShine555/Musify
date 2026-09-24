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
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, stranger, playList);

        var command = new UpdatePlayListCommand(stranger.Id, playList.Id, "Hijacked", null, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NewVisibility_UpdatesIt()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        await SeedAsync(owner, playList);

        var command = new UpdatePlayListCommand(owner.Id, playList.Id, null, null, null, Musify.Domain.ValueObjects.PlayListVisibility.Public);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(Musify.Domain.ValueObjects.PlayListVisibility.Public, result.Value.Visibility);
    }

    [Fact]
    public async Task Handle_NoVisibilitySpecified_LeavesItUnchanged()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id, visibility: Musify.Domain.ValueObjects.PlayListVisibility.Public);
        await SeedAsync(owner, playList);

        var command = new UpdatePlayListCommand(owner.Id, playList.Id, "New Name", null, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(Musify.Domain.ValueObjects.PlayListVisibility.Public, result.Value.Visibility);
    }

    [Fact]
    public async Task Handle_BlankName_LeavesItUnchangedAndBlankDescriptionClearsIt()
    {
        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id, "Original Name", "Original description");
        await SeedAsync(owner, playList);

        var command = new UpdatePlayListCommand(owner.Id, playList.Id, "   ", "   ", null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("Original Name", result.Value.Name);
        Assert.Null(result.Value.Description);
    }

    [Fact]
    public async Task Handle_PlayListWithoutPictures_ValidPictureIntent_SetsPictureAndPublishesSourceEvent()
    {
        storageService
            .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 1024));

        var owner = TestEntities.User();
        var playList = TestEntities.PlayList(owner.Id);
        playList.Pictures = null;
        var intent = TestEntities.UploadIntent(owner.Id, Musify.Domain.ValueObjects.UploadIntentPurpose.PlayListPicture, objectName: "new-cover.webp");
        await SeedAsync(owner, playList, intent);

        var command = new UpdatePlayListCommand(owner.Id, playList.Id, null, null, intent.Id);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Null(result.Value.SmallImageKeyName);
        var stored = await Database.PlayLists.FindAsync([playList.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal("new-cover.webp", stored.Pictures?.OriginalName);
        await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.UpdatePlayListPictureSourceEvent>(), Arg.Any<CancellationToken>());
    }
}
