using System.Text.Json;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Musify.Infrastructure.Tests.TestSupport;

/// <summary>
/// One Postgres container and one SeaweedFS container (same image and startup
/// command as <c>deploy/compose.yml</c>), started once for every test in the
/// <see cref="InfrastructureCollection"/> and torn down when the run finishes.
/// EF Core migrations run once here too, against the real <c>Database</c>
/// DbContext, so a broken migration fails the whole run immediately.
/// </summary>
public sealed class InfrastructureTestFixture : IAsyncLifetime
{
    public const string S3AccessKey = "musify-test";
    public const string S3SecretKey = "musify-test-secret";
    public const string S3Bucket = "musify-test-bucket";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("musify_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly IContainer seaweedFs = new ContainerBuilder("chrislusf/seaweedfs:latest")
        .WithEntrypoint("/bin/sh", "-c")
        .WithCommand(BuildSeaweedFsStartupScript())
        .WithPortBinding(9333, assignRandomHostPort: true)
        .WithPortBinding(8333, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request.ForPort(9333).ForPath("/cluster/healthz")))
        .Build();

    public string ConnectionString => postgres.GetConnectionString();

    public string S3ServiceUrl => $"http://{seaweedFs.Hostname}:{seaweedFs.GetMappedPublicPort(8333)}";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), seaweedFs.StartAsync());

        await using var database = new Database(new DatabaseConfiguration { ConnectionString = ConnectionString });
        await database.Database.MigrateAsync();

        // SeaweedFS does not auto-create buckets (see deploy/compose.yml's seaweedfs-init).
        using var s3 = CreateS3Client();
        await s3.PutBucketAsync(S3Bucket);
    }

    public async Task DisposeAsync()
    {
        await postgres.DisposeAsync();
        await seaweedFs.DisposeAsync();
    }

    /// <summary>A fresh, DI-wired <see cref="Database"/>, so <c>AuditableEntityInterceptor</c>
    /// (attached in <c>AddDatabase</c>'s DI registration, not in the class itself) is active —
    /// same wiring the real app uses, not a bare <c>new Database(...)</c>.</summary>
    public Database CreateDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Database:ConnectionString", ConnectionString)])
            .Build();

        var services = new ServiceCollection();
        services.AddDatabase(configuration);
        return services.BuildServiceProvider().GetRequiredService<Database>();
    }

    /// <summary>An <see cref="IAmazonS3"/> client configured exactly like
    /// <c>ServicesDependencyInjection.AddStorageService</c>, pointed at the SeaweedFS container.</summary>
    public IAmazonS3 CreateS3Client() =>
        new AmazonS3Client(S3AccessKey, S3SecretKey, new AmazonS3Config
        {
            ServiceURL = S3ServiceUrl,
            ForcePathStyle = true,
            UseHttp = true,
        });

    private static string BuildSeaweedFsStartupScript()
    {
        var identity = JsonSerializer.Serialize(new
        {
            identities = new[]
            {
                new
                {
                    name = "musify-test",
                    credentials = new[] { new { accessKey = S3AccessKey, secretKey = S3SecretKey } },
                    actions = new[] { "Admin", "Read", "Write" }
                }
            }
        });

        return $"""
            cat > /tmp/s3.conf <<'JSON'
            {identity}
            JSON
            exec weed server -ip.bind=0.0.0.0 -dir=/data -master.volumeSizeLimitMB=1024 -volume.max=0 -filer -s3 -s3.config=/tmp/s3.conf
            """;
    }
}

[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<InfrastructureTestFixture>
{
    public const string Name = "Infrastructure";
}
