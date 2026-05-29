using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Services;
using Musify.Application.Tests.Infrastructure;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Handlers;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class CreateTrackCommandHandlerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private CreateTrackCommandHandler CreateSut(TestDatabase db) =>
        new(
            db,
            _eventBus,
            new UploadIntentValidator(db, _storage),
            NullLogger<CreateTrackCommandHandler>.Instance,
            TestData.StorageConfig(),
            TestData.TrackConfig(),
            TestData.UploadIntentConfig());

    private void StorageHasObject() =>
        _storage.HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("application/octet-stream", 500));

    private static async Task<(UploadIntent picture, UploadIntent audio)> SeedIntentsAsync(
        SqliteTestContext ctx, Guid userId)
    {
        var picture = TestData.UploadIntent(userId, UploadIntentPurpose.TrackPicture, objectName: "pic.png", key: $"temp/{userId}/pic.png");
        var audio = TestData.UploadIntent(userId, UploadIntentPurpose.TrackAudio, objectName: "song.mp3", key: $"temp/{userId}/song.mp3");
        ctx.Database.UploadIntents.AddRange(picture, audio);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);
        return (picture, audio);
    }

    [Fact]
    public async Task When_user_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.Handle(
            new CreateTrackCommand(Guid.NewGuid(), "Song", Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task When_picture_intent_invalid_propagates_and_creates_no_track()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new CreateTrackCommand(user.Id, "Song", Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
        (await ctx.NewContext().Tracks.CountAsync()).Should().Be(0);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<CreateTrackResourcesEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_audio_intent_invalid_propagates()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        var picture = TestData.UploadIntent(user.Id, UploadIntentPurpose.TrackPicture, objectName: "pic.png", key: $"temp/{user.Id}/pic.png");
        ctx.Database.Users.Add(user);
        ctx.Database.UploadIntents.Add(picture);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        StorageHasObject();

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new CreateTrackCommand(user.Id, "Song", picture.Id, Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Happy_path_creates_track_publishes_event_and_normalizes_title()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);
        var (picture, audio) = await SeedIntentsAsync(ctx, user.Id);
        StorageHasObject();

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new CreateTrackCommand(user.Id, "My Song", picture.Id, audio.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Created);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<CreateTrackResourcesEvent>(e =>
                e.PictureIntentId == picture.Id && e.AudioIntentId == audio.Id),
            Arg.Any<CancellationToken>());

        var persisted = await ctx.NewContext().Tracks.SingleAsync();
        persisted.NormalizedTitle.Should().Be("MY SONG");
        persisted.OriginalPictureName.Should().Be("pic.png");
        persisted.OriginalAudioName.Should().Be("song.mp3");
    }

    [Fact]
    public async Task When_publish_throws_returns_error_and_does_not_persist()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);
        var (picture, audio) = await SeedIntentsAsync(ctx, user.Id);
        StorageHasObject();

        _eventBus.PublishAsync(Arg.Any<CreateTrackResourcesEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("bus down"));

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(
            new CreateTrackCommand(user.Id, "My Song", picture.Id, audio.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Error);
        (await ctx.NewContext().Tracks.CountAsync()).Should().Be(0);
    }
}
