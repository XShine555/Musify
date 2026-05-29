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

public sealed class CreatePlayListCommandHandlerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private CreatePlayListCommandHandler CreateSut(TestDatabase db) =>
        new(
            _eventBus,
            db,
            new UploadIntentValidator(db, _storage),
            NullLogger<CreatePlayListCommandHandler>.Instance,
            TestData.StorageConfig(),
            TestData.PlayListConfig(),
            TestData.UploadIntentConfig());

    [Fact]
    public async Task When_user_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.Handle(
            new CreatePlayListCommand(Guid.NewGuid(), "List", "desc", null), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Without_picture_intent_uses_preset_and_does_not_publish()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new CreatePlayListCommand(user.Id, "  Chill Vibes ", "desc", null), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Created);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<CreatePlayListResourcesEvent>(), Arg.Any<CancellationToken>());

        var persisted = await ctx.NewContext().PlayLists.SingleAsync(p => p.UserId == user.Id);
        persisted.OriginalPictureName.Should().Be(TestData.PlayListConfig().Routes.PresetOriginalPicture);
        persisted.NormalizedName.Should().Be("CHILL VIBES");
    }

    [Fact]
    public async Task With_valid_picture_intent_publishes_event_with_intent_object_name()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        var intent = TestData.UploadIntent(user.Id, UploadIntentPurpose.PlayListPicture, objectName: "cover.webp");
        ctx.Database.Users.Add(user);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.HeadObjectAsync(intent.Bucket, intent.Key, Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 500));

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new CreatePlayListCommand(user.Id, "List", "desc", intent.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Created);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<CreatePlayListResourcesEvent>(e => e.PictureIntentId == intent.Id),
            Arg.Any<CancellationToken>());

        var persisted = await ctx.NewContext().PlayLists.SingleAsync(p => p.UserId == user.Id);
        persisted.OriginalPictureName.Should().Be("cover.webp");
    }

    [Fact]
    public async Task With_invalid_picture_intent_propagates_error_and_does_not_persist()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new CreatePlayListCommand(user.Id, "List", "desc", Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
        (await ctx.NewContext().PlayLists.CountAsync()).Should().Be(0);
    }
}
