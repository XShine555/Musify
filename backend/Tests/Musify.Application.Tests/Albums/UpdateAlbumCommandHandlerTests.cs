using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Albums;

public sealed class UpdateAlbumCommandHandlerTests : HandlerTestBase
{
    private readonly IEventBus eventBus = Substitute.For<IEventBus>();
    private readonly IStorageService storageService = Substitute.For<IStorageService>();

    private UpdateAlbumCommandHandler CreateHandler() => new(
        eventBus,
        Database,
        new UploadIntentValidator(Database, storageService),
        NoOpLogger<UpdateAlbumCommandHandler>(),
        TestConfigurations.Storage(),
        TestConfigurations.Album(),
        TestConfigurations.UploadIntent());

    [Fact]
    public async Task Handle_Owner_UpdatesTitleDescriptionAndReleaseYear()
    {
        var owner = TestEntities.User();
        var album = TestEntities.Album(owner.Id, title: "Old Title", description: "Old", releaseYear: 2020);
        await SeedAsync(owner, album);

        var command = new UpdateAlbumCommand(owner.Id, album.Id, "  New Title  ", "New description", 2025, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("New Title", result.Value.Title);
        Assert.Equal("New description", result.Value.Description);
        Assert.Equal(2025, result.Value.ReleaseYear);

        var stored = await Database.Albums.FindAsync([album.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal("NEW TITLE", stored.NormalizedTitle);
    }

    [Fact]
    public async Task Handle_ValidPictureIntent_UpdatesPictureAndPublishesSourceEvent()
    {
        storageService
            .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 1024));

        var owner = TestEntities.User();
        var album = TestEntities.Album(owner.Id, title: "Old Title");
        var intent = TestEntities.UploadIntent(owner.Id, Musify.Domain.ValueObjects.UploadIntentPurpose.AlbumPicture, objectName: "new-cover.webp");
        await SeedAsync(owner, album, intent);

        var command = new UpdateAlbumCommand(owner.Id, album.Id, "Old Title", null, null, intent.Id);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var stored = await Database.Albums.FindAsync([album.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal("new-cover.webp", stored.Pictures?.OriginalName);
        await eventBus.Received(1).PublishAsync(Arg.Any<Musify.Application.Events.UpdateAlbumPictureSourceEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlbumMissing_ReturnsNotFound()
    {
        var command = new UpdateAlbumCommand(1, Guid.NewGuid(), "Title", null, null, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var owner = TestEntities.User(1, "owner");
        var stranger = TestEntities.User(2, "stranger");
        var album = TestEntities.Album(owner.Id);
        await SeedAsync(owner, stranger, album);

        var command = new UpdateAlbumCommand(stranger.Id, album.Id, "Hijacked", null, null, null);

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }
}
