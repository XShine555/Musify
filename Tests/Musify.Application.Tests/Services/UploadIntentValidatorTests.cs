using Ardalis.Result;
using FluentAssertions;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.Infrastructure;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Services;

public sealed class UploadIntentValidatorTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();

    private UploadIntentValidator CreateSut(TestDatabase database) => new(database, _storage);

    [Fact]
    public async Task CheckQuota_under_limits_returns_success()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var config = TestData.UploadIntentConfig();
        var sut = CreateSut(ctx.Database);

        var result = await sut.CheckQuotaAsync(config, userId, requiredBytes: 1_000, requiredIntentCount: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckQuota_when_intent_count_would_exceed_max_returns_invalid()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var config = TestData.UploadIntentConfig();
        config.MaxActiveUploadIntentsPerUser = 2;

        ctx.Database.UploadIntents.Add(TestData.UploadIntent(userId, expectedSizeBytes: 1));
        ctx.Database.UploadIntents.Add(TestData.UploadIntent(userId, expectedSizeBytes: 1));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.CheckQuotaAsync(config, userId, requiredBytes: 1, requiredIntentCount: 1, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task CheckQuota_exactly_at_intent_limit_is_allowed()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var config = TestData.UploadIntentConfig();
        config.MaxActiveUploadIntentsPerUser = 2;

        ctx.Database.UploadIntents.Add(TestData.UploadIntent(userId, expectedSizeBytes: 1));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.CheckQuotaAsync(config, userId, requiredBytes: 1, requiredIntentCount: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckQuota_when_byte_quota_exceeded_returns_invalid()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var config = TestData.UploadIntentConfig();
        config.MaxActiveUploadBytesPerUser = 1_000;

        ctx.Database.UploadIntents.Add(TestData.UploadIntent(userId, expectedSizeBytes: 800));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.CheckQuotaAsync(config, userId, requiredBytes: 300, requiredIntentCount: 1, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task CheckQuota_uses_default_picture_size_for_intents_without_expected_size()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var config = TestData.UploadIntentConfig();
        config.MaxActiveUploadBytesPerUser = 1_000;
        config.DefaultExpectedPictureSizeBytes = 900;

        ctx.Database.UploadIntents.Add(TestData.UploadIntent(userId, expectedSizeBytes: null));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.CheckQuotaAsync(config, userId, requiredBytes: 200, requiredIntentCount: 1, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task CheckQuota_ignores_other_users_and_non_issued_intents()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var config = TestData.UploadIntentConfig();
        config.MaxActiveUploadIntentsPerUser = 2;
        config.MaxActiveUploadBytesPerUser = 1_000;

        ctx.Database.UploadIntents.Add(TestData.UploadIntent(otherUser, expectedSizeBytes: 5_000));
        ctx.Database.UploadIntents.Add(TestData.UploadIntent(userId, status: UploadIntentStatus.Consumed, expectedSizeBytes: 5_000));
        ctx.Database.UploadIntents.Add(TestData.UploadIntent(userId, status: UploadIntentStatus.Expired, expectedSizeBytes: 5_000));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.CheckQuotaAsync(config, userId, requiredBytes: 100, requiredIntentCount: 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndLoad_when_intent_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.ValidateAndLoadAsync(
            TestData.UploadIntentConfig(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ValidateAndLoad_when_intent_belongs_to_other_user_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var ownerId = Guid.NewGuid();
        var intent = TestData.UploadIntent(ownerId);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.ValidateAndLoadAsync(
            TestData.UploadIntentConfig(), intent.Id, Guid.NewGuid(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ValidateAndLoad_when_consumed_returns_conflict()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var intent = TestData.UploadIntent(userId, status: UploadIntentStatus.Consumed);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.ValidateAndLoadAsync(
            TestData.UploadIntentConfig(), intent.Id, userId, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task ValidateAndLoad_when_status_expired_returns_invalid()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var intent = TestData.UploadIntent(userId, status: UploadIntentStatus.Expired);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.ValidateAndLoadAsync(
            TestData.UploadIntentConfig(), intent.Id, userId, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task ValidateAndLoad_when_expired_by_timestamp_returns_invalid()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var intent = TestData.UploadIntent(userId, status: UploadIntentStatus.Issued, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.ValidateAndLoadAsync(
            TestData.UploadIntentConfig(), intent.Id, userId, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task ValidateAndLoad_when_object_missing_in_storage_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var intent = TestData.UploadIntent(userId);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.HeadObjectAsync(intent.Bucket, intent.Key, Arg.Any<CancellationToken>())
            .Returns((ObjectMetaData?)null);

        var sut = CreateSut(ctx.Database);
        var result = await sut.ValidateAndLoadAsync(
            TestData.UploadIntentConfig(), intent.Id, userId, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ValidateAndLoad_when_object_exceeds_max_size_returns_invalid()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var config = TestData.UploadIntentConfig();
        config.MaxUploadBytes = 1_000;
        var intent = TestData.UploadIntent(userId);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.HeadObjectAsync(intent.Bucket, intent.Key, Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 2_000));

        var sut = CreateSut(ctx.Database);
        var result = await sut.ValidateAndLoadAsync(config, intent.Id, userId, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task ValidateAndLoad_happy_path_returns_loaded_intent()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var intent = TestData.UploadIntent(userId);
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _storage.HeadObjectAsync(intent.Bucket, intent.Key, Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 500));

        var sut = CreateSut(ctx.Database);
        var result = await sut.ValidateAndLoadAsync(
            TestData.UploadIntentConfig(), intent.Id, userId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(intent.Id);
    }
}
