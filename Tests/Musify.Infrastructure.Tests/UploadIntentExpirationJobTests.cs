using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Tests.Infrastructure;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Jobs;
using Xunit;

namespace Musify.Infrastructure.Tests;

public sealed class UploadIntentExpirationJobTests
{
    private static UploadIntentExpirationJob CreateJob(SqliteTestContext ctx, Action<Application.Configuration.UploadIntentConfiguration>? configure = null)
    {
        var config = TestData.UploadIntentConfig();
        configure?.Invoke(config);
        return new UploadIntentExpirationJob(
            JobTestHarness.WithDatabase(ctx.Database),
            NullLogger<UploadIntentExpirationJob>.Instance,
            config);
    }

    [Fact]
    public async Task Marks_issued_intents_past_expiry_as_expired()
    {
        await using var ctx = new SqliteTestContext();
        var expired = TestData.UploadIntent(Guid.NewGuid(), status: UploadIntentStatus.Issued, expiresAt: DateTime.UtcNow.AddMinutes(-5));
        var active = TestData.UploadIntent(Guid.NewGuid(), status: UploadIntentStatus.Issued, expiresAt: DateTime.UtcNow.AddMinutes(30));
        ctx.Database.UploadIntents.AddRange(expired, active);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        await JobTestHarness.RunAsync(CreateJob(ctx));

        var verify = ctx.NewContext();
        (await verify.UploadIntents.SingleAsync(i => i.Id == expired.Id)).Status.Should().Be(UploadIntentStatus.Expired);
        (await verify.UploadIntents.SingleAsync(i => i.Id == active.Id)).Status.Should().Be(UploadIntentStatus.Issued);
    }

    [Fact]
    public async Task Deletes_expired_intents_past_retention_window()
    {
        await using var ctx = new SqliteTestContext();
        var config = TestData.UploadIntentConfig();
        var old = TestData.UploadIntent(Guid.NewGuid(), status: UploadIntentStatus.Expired,
            expiresAt: DateTime.UtcNow.AddDays(-(config.ExpiredIntentsRetentionDays + 1)));
        var recent = TestData.UploadIntent(Guid.NewGuid(), status: UploadIntentStatus.Expired,
            expiresAt: DateTime.UtcNow.AddHours(-1));
        ctx.Database.UploadIntents.AddRange(old, recent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        await JobTestHarness.RunAsync(CreateJob(ctx));

        var verify = ctx.NewContext();
        (await verify.UploadIntents.AnyAsync(i => i.Id == old.Id)).Should().BeFalse();
        (await verify.UploadIntents.AnyAsync(i => i.Id == recent.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task With_zero_retention_just_expired_intents_are_marked_then_deleted()
    {
        await using var ctx = new SqliteTestContext();
        var intent = TestData.UploadIntent(Guid.NewGuid(), status: UploadIntentStatus.Issued, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        ctx.Database.UploadIntents.Add(intent);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        await JobTestHarness.RunAsync(CreateJob(ctx, c => c.ExpiredIntentsRetentionDays = 0));

        (await ctx.NewContext().UploadIntents.AnyAsync(i => i.Id == intent.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Leaves_consumed_intents_untouched()
    {
        await using var ctx = new SqliteTestContext();
        var consumed = TestData.UploadIntent(Guid.NewGuid(), status: UploadIntentStatus.Consumed, expiresAt: DateTime.UtcNow.AddDays(-10));
        ctx.Database.UploadIntents.Add(consumed);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        await JobTestHarness.RunAsync(CreateJob(ctx));

        (await ctx.NewContext().UploadIntents.SingleAsync(i => i.Id == consumed.Id)).Status
            .Should().Be(UploadIntentStatus.Consumed);
    }
}
