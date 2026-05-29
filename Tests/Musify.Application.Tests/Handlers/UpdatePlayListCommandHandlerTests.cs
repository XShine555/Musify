using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Handlers;
using Musify.Application.Services;
using Musify.Application.Tests.Infrastructure;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class UpdatePlayListCommandHandlerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private UpdatePlayListCommandHandler CreateSut(TestDatabase db) =>
        new(
            _eventBus,
            db,
            new UploadIntentValidator(db, _storage),
            NullLogger<UpdatePlayListCommandHandler>.Instance,
            TestData.StorageConfig(),
            TestData.PlayListConfig(),
            TestData.UploadIntentConfig());

    [Fact]
    public async Task When_playlist_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.Handle(
            new UpdatePlayListCommand(Guid.NewGuid(), Guid.NewGuid(), "n", null, null), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task When_user_not_owner_returns_unauthorized()
    {
        await using var ctx = new SqliteTestContext();
        var playList = TestData.PlayList(Guid.NewGuid());
        ctx.Database.PlayLists.Add(playList);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new UpdatePlayListCommand(Guid.NewGuid(), playList.Id, "n", null, null), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Updates_name_and_description_without_publishing()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var playList = TestData.PlayList(userId, name: "Old");
        ctx.Database.PlayLists.Add(playList);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new UpdatePlayListCommand(userId, playList.Id, " New Name ", "new desc", null), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Ok);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<UpdatePlayListPictureSourceEvent>(), Arg.Any<CancellationToken>());

        var persisted = await ctx.NewContext().PlayLists.SingleAsync(p => p.Id == playList.Id);
        persisted.Name.Should().Be(" New Name ");
        persisted.NormalizedName.Should().Be("NEW NAME");
        persisted.Description.Should().Be("new desc");
    }

    [Fact]
    public async Task When_only_description_provided_name_is_unchanged()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var playList = TestData.PlayList(userId, name: "Keep");
        ctx.Database.PlayLists.Add(playList);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new UpdatePlayListCommand(userId, playList.Id, null, "only desc", null), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Ok);
        var persisted = await ctx.NewContext().PlayLists.SingleAsync(p => p.Id == playList.Id);
        persisted.Name.Should().Be("Keep");
        persisted.Description.Should().Be("only desc");
    }

    [Fact]
    public async Task With_valid_new_picture_intent_publishes_source_event_and_updates_picture_name()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var playList = TestData.PlayList(userId);
        var intent = TestData.UploadIntent(userId, UploadIntentPurpose.PlayListPicture, objectName: "newcover.webp");
        ctx.Database.PlayLists.Add(playList);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.HeadObjectAsync(intent.Bucket, intent.Key, Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 500));

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new UpdatePlayListCommand(userId, playList.Id, null, null, intent.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Ok);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UpdatePlayListPictureSourceEvent>(e => e.PlayListId == playList.Id && e.PictureIntentId == intent.Id),
            Arg.Any<CancellationToken>());

        var persisted = await ctx.NewContext().PlayLists.SingleAsync(p => p.Id == playList.Id);
        persisted.OriginalPictureName.Should().Be("newcover.webp");
    }

    [Fact]
    public async Task With_invalid_new_picture_intent_propagates_error()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var playList = TestData.PlayList(userId);
        var intent = TestData.UploadIntent(userId, UploadIntentPurpose.PlayListPicture, status: UploadIntentStatus.Consumed);
        ctx.Database.PlayLists.Add(playList);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new UpdatePlayListCommand(userId, playList.Id, null, null, intent.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
    }
}
