using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Contracts;
using Musify.Application.Tests.Infrastructure;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Services;
using Xunit;

namespace Musify.Infrastructure.IntegrationTests;

[Collection(SeaweedFsCollection.Name)]
public sealed class StorageServiceTests(SeaweedFsFixture seaweedFs)
{
    private const string Bucket = SeaweedFsFixture.Bucket;

    private (StorageService service, SqliteTestContext db) CreateService()
    {
        var db = new SqliteTestContext();
        var service = new StorageService(
            db.Database,
            seaweedFs.CreateClient(),
            NullLogger<StorageService>.Instance,
            seaweedFs.StorageConfig());
        return (service, db);
    }

    private static MemoryStream TextStream(string content) => new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task Upload_then_head_and_get_roundtrips_content()
    {
        var (service, db) = CreateService();
        using var _ = db;
        var key = $"tests/{Guid.NewGuid()}.txt";

        await service.UploadFileAsync(TextStream("hello seaweed"), "text/plain", Bucket, key, CancellationToken.None);

        var metadata = await service.HeadObjectAsync(Bucket, key, CancellationToken.None);
        metadata.Should().NotBeNull();
        metadata!.ContentLength.Should().Be("hello seaweed".Length);

        using var stream = await service.GetFileAsync(Bucket, key, CancellationToken.None);
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).Should().Be("hello seaweed");
    }

    [Fact]
    public async Task Upload_persists_a_successful_upload_record()
    {
        var (service, db) = CreateService();
        using var _ = db;
        var key = $"tests/{Guid.NewGuid()}.txt";

        await service.UploadFileAsync(TextStream("data"), "text/plain", Bucket, key, CancellationToken.None);

        var upload = db.NewContext().Uploads.Single(u => u.Key == key);
        upload.State.Should().Be(UploadState.Successful);
        upload.Bucket.Should().Be(Bucket);
    }

    [Fact]
    public async Task HeadObject_returns_null_for_missing_object()
    {
        var (service, db) = CreateService();
        using var _ = db;

        var metadata = await service.HeadObjectAsync(Bucket, $"tests/missing-{Guid.NewGuid()}", CancellationToken.None);

        metadata.Should().BeNull();
    }

    [Fact]
    public async Task CopyFile_duplicates_the_object()
    {
        var (service, db) = CreateService();
        using var _ = db;
        var source = $"tests/{Guid.NewGuid()}-src.txt";
        var destination = $"tests/{Guid.NewGuid()}-dst.txt";
        await service.UploadFileAsync(TextStream("copy me"), "text/plain", Bucket, source, CancellationToken.None);

        await service.CopyFileAsync(Bucket, source, Bucket, destination, CancellationToken.None);

        (await service.HeadObjectAsync(Bucket, destination, CancellationToken.None)).Should().NotBeNull();
        (await service.HeadObjectAsync(Bucket, source, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveFile_deletes_the_object()
    {
        var (service, db) = CreateService();
        using var _ = db;
        var key = $"tests/{Guid.NewGuid()}.txt";
        await service.UploadFileAsync(TextStream("delete me"), "text/plain", Bucket, key, CancellationToken.None);

        await service.RemoveFileAsync(Bucket, key, CancellationToken.None);

        (await service.HeadObjectAsync(Bucket, key, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ListObjects_returns_objects_under_prefix()
    {
        var (service, db) = CreateService();
        using var _ = db;
        var prefix = $"listing/{Guid.NewGuid()}";
        await service.UploadFileAsync(TextStream("a"), "text/plain", Bucket, $"{prefix}/a.txt", CancellationToken.None);
        await service.UploadFileAsync(TextStream("b"), "text/plain", Bucket, $"{prefix}/b.txt", CancellationToken.None);

        var keys = new List<string>();
        await foreach (var obj in service.ListObjectsAsync(Bucket, prefix, CancellationToken.None))
            keys.Add(obj.Key);

        keys.Should().Contain($"{prefix}/a.txt").And.Contain($"{prefix}/b.txt");
    }

    [Fact]
    public async Task RemoveFolder_deletes_every_object_under_prefix()
    {
        var (service, db) = CreateService();
        using var _ = db;
        var prefix = $"folder/{Guid.NewGuid()}";
        await service.UploadFileAsync(TextStream("a"), "text/plain", Bucket, $"{prefix}/a.txt", CancellationToken.None);
        await service.UploadFileAsync(TextStream("b"), "text/plain", Bucket, $"{prefix}/nested/b.txt", CancellationToken.None);

        await service.RemoveFolderAsync(Bucket, prefix, CancellationToken.None);

        var remaining = new List<StorageObject>();
        await foreach (var obj in service.ListObjectsAsync(Bucket, prefix, CancellationToken.None))
            remaining.Add(obj);
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUploadUrl_returns_a_presigned_url()
    {
        var (service, db) = CreateService();
        using var _ = db;

        var url = await service.GetUploadUrlAsync(
            Bucket, $"tests/{Guid.NewGuid()}.txt", "text/plain", TimeSpan.FromMinutes(5), CancellationToken.None);

        url.Should().StartWith(seaweedFs.ServiceUrl);
    }
}
