using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Musify.Infrastructure.Configuration;
using Xunit;

namespace Musify.Infrastructure.IntegrationTests;

public sealed class SeaweedFsFixture : IAsyncLifetime
{
    private const int S3Port = 8333;
    private const string AccessKey = "admin_access_key";
    private const string SecretKey = "admin_secret_key";

    public const string Bucket = "webapi-storage";

    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("chrislusf/seaweedfs:latest")
        .WithCommand("server", "-s3", "-dir=/data", "-ip.bind=0.0.0.0")
        .WithPortBinding(S3Port, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Start Seaweed S3 API Server"))
        .Build();

    public string ServiceUrl { get; private set; } = string.Empty;

    public IAmazonS3 CreateClient()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = ServiceUrl,
            ForcePathStyle = true,
            UseHttp = true,
            AuthenticationRegion = "us-east-1",
        };
        return new AmazonS3Client(AccessKey, SecretKey, config);
    }

    public InfrastructureStorageConfiguration StorageConfig() => new()
    {
        Address = ServiceUrl,
        AccessKey = AccessKey,
        SecretAccessKey = SecretKey,
        ForcePathStyle = true,
        UseHttp = true,
    };

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ServiceUrl = $"http://{_container.Hostname}:{_container.GetMappedPublicPort(S3Port)}";

        using var client = CreateClient();
        await client.PutBucketAsync(new PutBucketRequest { BucketName = Bucket });
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(SeaweedFsCollection.Name)]
public sealed class SeaweedFsCollection : ICollectionFixture<SeaweedFsFixture>
{
    public const string Name = "SeaweedFS";
}
