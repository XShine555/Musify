using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.PlayLists;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class CreatePlayListCommandHandlerTests : HandlerTestBase
{
    private readonly IEventBus eventBus = Substitute.For<IEventBus>();
    private readonly IStorageService storageService = Substitute.For<IStorageService>();

    private CreatePlayListCommandHandler CreateHandler() => new(
        eventBus,
        Database,
        new UploadIntentValidator(Database, storageService),
        NoOpLogger<CreatePlayListCommandHandler>(),
        TestConfigurations.Storage(),
        TestConfigurations.PlayList(),
        TestConfigurations.UploadIntent());

    [Fact]
    public async Task Handle_NoPictureIntent_CreatesPlayListWithPresetPictures()
    {
        var user = TestEntities.User();
        await SeedAsync(user);

        var command = new CreatePlayListCommand(user.Id, "My Playlist", "A description", PictureIntentId: null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("My Playlist", result.Value.Name);
        Assert.Equal("PresetSmallPicture.webp", result.Value.SmallImageKeyName);
        await eventBus.DidNotReceive().PublishAsync(Arg.Any<Musify.Application.Events.CreatePlayListResourcesEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidPictureIntent_PublishesResourcesEvent()
    {
        storageService
            .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 1024));

        var user = TestEntities.User();
        var intent = TestEntities.UploadIntent(user.Id, Musify.Domain.ValueObjects.UploadIntentPurpose.PlayListPicture, objectName: "cover.webp");
        await SeedAsync(user, intent);

        var command = new CreatePlayListCommand(user.Id, "My Playlist", null, intent.Id);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.False(result.IsError);
        var stored = await Database.PlayLists.FindAsync(result.Value.Id);
        Assert.NotNull(stored);
        Assert.Equal("cover.webp", stored.Pictures.OriginalName);
        await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.CreatePlayListResourcesEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        var command = new CreatePlayListCommand(404, "Orphan Playlist", null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_PictureIntentNotFound_ReturnsError()
    {
        var user = TestEntities.User();
        await SeedAsync(user);

        var command = new CreatePlayListCommand(user.Id, "My Playlist", null, Guid.NewGuid());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
    }
}
