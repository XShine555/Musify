using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.Infrastructure;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Handlers;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class RequestTrackUploadUrlsCommandHandlerTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private RequestTrackUploadUrlsCommandHandler CreateSut(TestDatabase db, UploadIntentConfiguration? config = null) =>
        new(
            db,
            new UploadIntentValidator(db, _storage),
            _storage,
            NullLogger<RequestTrackUploadUrlsCommandHandler>.Instance,
            TestData.StorageConfig(),
            TestData.TrackConfig(),
            config ?? TestData.UploadIntentConfig());

    private static RequestTrackUploadUrlsCommand Command(Guid userId, long? pic = null, long? audio = null) =>
        new(userId, ".PNG", "image/png", "mp3", "audio/mpeg", pic, audio);

    [Fact]
    public async Task When_user_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Happy_path_creates_two_intents_and_returns_urls()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.GetUploadUrlAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://signed.example/upload");

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(Command(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PictureUploadUrl.Should().Be("https://signed.example/upload");

        var intents = await ctx.NewContext().UploadIntents.Where(i => i.UserId == user.Id).ToListAsync();
        intents.Should().HaveCount(2);
        intents.Select(i => i.Purpose).Should().BeEquivalentTo(
            new[] { UploadIntentPurpose.TrackPicture, UploadIntentPurpose.TrackAudio });
        intents.Should().Contain(i => i.ObjectName.EndsWith(".png"));
        intents.Should().Contain(i => i.ObjectName.EndsWith(".mp3"));
    }

    [Fact]
    public async Task When_quota_exceeded_returns_invalid_and_creates_no_intents()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.GetUploadUrlAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://signed.example/upload");

        var config = TestData.UploadIntentConfig();
        config.MaxActiveUploadIntentsPerUser = 1;

        var sut = CreateSut(ctx.Database, config);
        var result = await sut.Handle(Command(user.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        (await ctx.NewContext().UploadIntents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Uses_default_sizes_when_not_provided()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.GetUploadUrlAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://signed.example/upload");

        var config = TestData.UploadIntentConfig();
        var sut = CreateSut(ctx.Database, config);
        await sut.Handle(Command(user.Id), CancellationToken.None);

        var intents = await ctx.NewContext().UploadIntents.Where(i => i.UserId == user.Id).ToListAsync();
        intents.Single(i => i.Purpose == UploadIntentPurpose.TrackPicture)
            .ExpectedSizeBytes.Should().Be(config.DefaultExpectedPictureSizeBytes);
        intents.Single(i => i.Purpose == UploadIntentPurpose.TrackAudio)
            .ExpectedSizeBytes.Should().Be(config.DefaultExpectedAudioSizeBytes);
    }

    [Fact]
    public async Task When_storage_throws_returns_error()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.GetUploadUrlAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("s3 down"));

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(Command(user.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Error);
        (await ctx.NewContext().UploadIntents.CountAsync()).Should().Be(0);
    }
}
