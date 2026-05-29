using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Contracts;
using Musify.Application.Tests.Infrastructure;
using Musify.Infrastructure.Jobs;
using NSubstitute;
using Xunit;

namespace Musify.Infrastructure.Tests;

public sealed class TemporalUploadsCleanUpJobTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly Application.Configuration.UploadIntentConfiguration _config = TestData.UploadIntentConfig();

    private TemporalUploadsCleanUpJob CreateJob() =>
        new(
            JobTestHarness.WithStorage(_storage),
            NullLogger<TemporalUploadsCleanUpJob>.Instance,
            TestData.StorageConfig(),
            _config);

    private static async IAsyncEnumerable<StorageObject> AsAsync(params StorageObject[] items)
    {
        foreach (var item in items)
            yield return item;
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Deletes_only_objects_older_than_retention_window()
    {
        var cutoffDays = _config.TempUploadsRetentionDays;
        var stale = new StorageObject("temp/u1/old.webp", DateTime.UtcNow.AddDays(-(cutoffDays + 2)));
        var fresh = new StorageObject("temp/u1/new.webp", DateTime.UtcNow.AddHours(-1));

        _storage.ListObjectsAsync(TestData.Bucket, _config.TempRootPrefix, Arg.Any<CancellationToken>())
            .Returns(AsAsync(stale, fresh));

        await JobTestHarness.RunAsync(CreateJob());

        await _storage.Received(1).RemoveFileAsync(TestData.Bucket, stale.Key, Arg.Any<CancellationToken>());
        await _storage.DidNotReceive().RemoveFileAsync(TestData.Bucket, fresh.Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Continues_when_a_delete_fails()
    {
        var first = new StorageObject("temp/u1/a.webp", DateTime.UtcNow.AddDays(-10));
        var second = new StorageObject("temp/u1/b.webp", DateTime.UtcNow.AddDays(-10));

        _storage.ListObjectsAsync(TestData.Bucket, _config.TempRootPrefix, Arg.Any<CancellationToken>())
            .Returns(AsAsync(first, second));
        _storage.RemoveFileAsync(TestData.Bucket, first.Key, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("s3 down"));

        await JobTestHarness.RunAsync(CreateJob());

        await _storage.Received(1).RemoveFileAsync(TestData.Bucket, second.Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_nothing_when_no_objects_listed()
    {
        _storage.ListObjectsAsync(TestData.Bucket, _config.TempRootPrefix, Arg.Any<CancellationToken>())
            .Returns(AsAsync());

        await JobTestHarness.RunAsync(CreateJob());

        await _storage.DidNotReceive().RemoveFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
