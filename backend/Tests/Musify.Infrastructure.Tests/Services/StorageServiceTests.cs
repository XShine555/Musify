using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Services;
using Musify.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Musify.Infrastructure.Tests.Services;

[Collection(InfrastructureCollection.Name)]
public sealed class StorageServiceTests(InfrastructureTestFixture fixture) : IAsyncLifetime
{
    private Database database = null!;
    private StorageService service = null!;

    public ValueTask InitializeAsync()
    {
        database = fixture.CreateDatabase();
        var storageConfiguration = new InfrastructureStorageConfiguration
        {
            Address = fixture.S3ServiceUrl,
            AccessKey = InfrastructureTestFixture.S3AccessKey,
            SecretAccessKey = InfrastructureTestFixture.S3SecretKey,
            ForcePathStyle = true,
            UseHttp = true,
        };
        service = new StorageService(database, fixture.CreateS3Client(), NullLogger<StorageService>.Instance, storageConfiguration);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => database.DisposeAsync();

    [Fact]
    public async Task UploadThenGetFile_RoundTripsTheSameBytes()
    {
        var key = $"tests/{Guid.NewGuid():N}/round-trip.txt";
        var content = "hello seaweedfs"u8.ToArray();

        await service.UploadFileAsync(new MemoryStream(content), "text/plain", InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);

        await using var stream = await service.GetFileAsync(InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var readBack = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Equal("hello seaweedfs", readBack);
    }

    [Fact]
    public async Task HeadObjectAsync_ExistingObject_ReturnsMetadata()
    {
        var key = $"tests/{Guid.NewGuid():N}/head.txt";
        await service.UploadFileAsync(new MemoryStream("x"u8.ToArray()), "text/plain", InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);

        var metadata = await service.HeadObjectAsync(InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);

        Assert.NotNull(metadata);
        Assert.Equal(1, metadata.ContentLength);
    }

    [Fact]
    public async Task HeadObjectAsync_MissingObject_ReturnsNull()
    {
        var metadata = await service.HeadObjectAsync(InfrastructureTestFixture.S3Bucket, $"tests/{Guid.NewGuid():N}/missing.txt", TestContext.Current.CancellationToken);

        Assert.Null(metadata);
    }

    [Fact]
    public async Task RemoveFileAsync_ExistingObject_DeletesIt()
    {
        var key = $"tests/{Guid.NewGuid():N}/to-remove.txt";
        await service.UploadFileAsync(new MemoryStream("x"u8.ToArray()), "text/plain", InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);

        await service.RemoveFileAsync(InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);

        Assert.Null(await service.HeadObjectAsync(InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CopyFileAsync_CopiesToTheDestinationKey()
    {
        var sourceKey = $"tests/{Guid.NewGuid():N}/source.txt";
        var destinationKey = $"tests/{Guid.NewGuid():N}/destination.txt";
        await service.UploadFileAsync(new MemoryStream("copy-me"u8.ToArray()), "text/plain", InfrastructureTestFixture.S3Bucket, sourceKey, TestContext.Current.CancellationToken);

        await service.CopyFileAsync(InfrastructureTestFixture.S3Bucket, sourceKey, InfrastructureTestFixture.S3Bucket, destinationKey, TestContext.Current.CancellationToken);

        Assert.NotNull(await service.HeadObjectAsync(InfrastructureTestFixture.S3Bucket, destinationKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListObjectsAsync_ReturnsEveryObjectUnderThePrefix()
    {
        var prefix = $"tests/{Guid.NewGuid():N}";
        await service.UploadFileAsync(new MemoryStream("a"u8.ToArray()), "text/plain", InfrastructureTestFixture.S3Bucket, $"{prefix}/a.txt", TestContext.Current.CancellationToken);
        await service.UploadFileAsync(new MemoryStream("b"u8.ToArray()), "text/plain", InfrastructureTestFixture.S3Bucket, $"{prefix}/b.txt", TestContext.Current.CancellationToken);

        var keys = new List<string>();
        await foreach (var item in service.ListObjectsAsync(InfrastructureTestFixture.S3Bucket, prefix, TestContext.Current.CancellationToken))
            keys.Add(item.Key);

        Assert.Equal([$"{prefix}/a.txt", $"{prefix}/b.txt"], keys.Order());
    }

    [Fact]
    public async Task RemoveFolderAsync_DeletesEveryObjectUnderThePrefix()
    {
        var prefix = $"tests/{Guid.NewGuid():N}";
        await service.UploadFileAsync(new MemoryStream("a"u8.ToArray()), "text/plain", InfrastructureTestFixture.S3Bucket, $"{prefix}/a.txt", TestContext.Current.CancellationToken);
        await service.UploadFileAsync(new MemoryStream("b"u8.ToArray()), "text/plain", InfrastructureTestFixture.S3Bucket, $"{prefix}/nested/b.txt", TestContext.Current.CancellationToken);

        await service.RemoveFolderAsync(InfrastructureTestFixture.S3Bucket, prefix, TestContext.Current.CancellationToken);

        var remaining = new List<string>();
        await foreach (var item in service.ListObjectsAsync(InfrastructureTestFixture.S3Bucket, prefix, TestContext.Current.CancellationToken))
            remaining.Add(item.Key);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task GetUploadUrlAsync_PresignedPut_CanActuallyUploadTheObject()
    {
        var key = $"tests/{Guid.NewGuid():N}/presigned.txt";

        var uploadUrl = await service.GetUploadUrlAsync(
            InfrastructureTestFixture.S3Bucket, key, "text/plain", TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            // Not StringContent: it appends "; charset=utf-8" to Content-Type, which no longer
            // matches the "text/plain" the presigned URL's signature was computed over (SigV4
            // signs Content-Type byte-for-byte) and the server answers 403 SignatureDoesNotMatch.
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("presigned content"))
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        // GetUploadUrlAsync defaults preventOverwrite to true, which signs the URL together with
        // this header (see StorageService.GetUploadUrlAsync) — the actual PUT has to send it too,
        // or the signature no longer matches and the server answers 403.
        request.Headers.TryAddWithoutValidation("If-None-Match", "*");
        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metadata = await service.HeadObjectAsync(InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);
        Assert.NotNull(metadata);
        Assert.Equal("presigned content".Length, metadata.ContentLength);
    }
}
