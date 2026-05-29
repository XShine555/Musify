using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Handlers;
using Musify.Application.Services;
using Musify.Application.Tests.Infrastructure;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class RequestPlayListPictureUploadCommandHandlerTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private RequestPlayListPictureUploadCommandHandler CreateSut(TestDatabase db, UploadIntentConfiguration? config = null) =>
        new(
            db,
            new UploadIntentValidator(db, _storage),
            _storage,
            NullLogger<RequestPlayListPictureUploadCommandHandler>.Instance,
            TestData.StorageConfig(),
            TestData.PlayListConfig(),
            config ?? TestData.UploadIntentConfig());

    private static RequestPlayListPictureUploadCommand Command(Guid userId, long? size = null) =>
        new(userId, ".WEBP", "image/webp", size);

    [Fact]
    public async Task When_user_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Happy_path_creates_single_intent_and_returns_url()
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
        result.Value.UploadUrl.Should().Be("https://signed.example/upload");

        var intents = await ctx.NewContext().UploadIntents.Where(i => i.UserId == user.Id).ToListAsync();
        intents.Should().ContainSingle();
        intents[0].Purpose.Should().Be(UploadIntentPurpose.PlayListPicture);
        intents[0].ObjectName.Should().EndWith(".webp");
    }

    [Fact]
    public async Task When_quota_exceeded_returns_invalid_and_creates_no_intent()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.GetUploadUrlAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://signed.example/upload");

        var config = TestData.UploadIntentConfig();
        config.MaxActiveUploadIntentsPerUser = 0;

        var sut = CreateSut(ctx.Database, config);
        var result = await sut.Handle(Command(user.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        (await ctx.NewContext().UploadIntents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Uses_default_picture_size_when_not_provided()
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

        var intent = await ctx.NewContext().UploadIntents.SingleAsync(i => i.UserId == user.Id);
        intent.ExpectedSizeBytes.Should().Be(config.DefaultExpectedPictureSizeBytes);
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
