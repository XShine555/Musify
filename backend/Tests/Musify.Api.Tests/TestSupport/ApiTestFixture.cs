using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Musify.Application.Contracts;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace Musify.Api.Tests.TestSupport;

public sealed class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("musify_api_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IStorageService StorageService { get; } = Substitute.For<IStorageService>();

    public IEventBus EventBus { get; } = Substitute.For<IEventBus>();

    public IStreamTicketService StreamTicketService { get; } = Substitute.For<IStreamTicketService>();

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();

        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Musify.Infrastructure.Persistence.Database>();
        await database.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await postgres.DisposeAsync();
    }

    public HttpClient CreateAnonymousClient() => CreateClient();

    public async Task<long> SeedUserAsync(long? userId = null, string? name = null)
    {
        var id = userId ?? Random.Shared.NextInt64(1, long.MaxValue);
        var userName = name ?? $"user-{id}";

        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Musify.Infrastructure.Persistence.Database>();
        database.Users.Add(new Musify.Domain.Entities.User
        {
            Id = id,
            Name = userName,
            NormalizedName = userName.ToUpperInvariant()
        });
        await database.SaveChangesAsync();
        return id;
    }

    public HttpClient CreateAuthenticatedClient(long userId, string userName = "test-user")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(FakeAuthenticationHandler.UserIdHeader, userId.ToString(CultureInfo.InvariantCulture));
        client.DefaultRequestHeaders.Add("X-Test-User-Name", userName);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = postgres.GetConnectionString(),
                ["Authentication:MetadataAddress"] = "https://auth.musify.test/.well-known/openid-configuration",
                ["Authentication:IssuerAddress"] = "https://auth.musify.test",
                ["Authentication:AudienceAddress"] = "musify-api-test",
                ["Authentication:ClientId"] = "musify-api-test",
                ["Authentication:AuthorizationEndpoint"] = "https://auth.musify.test/authorize",
                ["Authentication:TokenEndpoint"] = "https://auth.musify.test/token",
                ["Authentication:Scopes:0"] = "openid",
                ["StreamTicket:PrivateKeyPath"] = "unused-in-tests.pem",
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317",
                ["MassTransit:Address"] = "rabbitmq://localhost:5672",
                ["MassTransit:Username"] = "guest",
                ["MassTransit:Password"] = "guest",
                ["InfrastructureStorage:Address"] = "http://localhost:8333",
                ["InfrastructureStorage:AccessKey"] = "unused-in-tests",
                ["InfrastructureStorage:SecretAccessKey"] = "unused-in-tests",
                ["InfrastructureStorage:ForcePathStyle"] = "true",
                ["InfrastructureStorage:UseHttp"] = "true",
                ["ApplicationStorage:Bucket"] = "musify-api-test",
                ["StreamGateway:PublicBaseUrl"] = "http://stream.musify.test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton(StorageService));
            services.Replace(ServiceDescriptor.Scoped(_ => EventBus));
            services.Replace(ServiceDescriptor.Singleton(StreamTicketService));

            services
                .AddAuthentication(FakeAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>(FakeAuthenticationHandler.SchemeName, _ => { });
        });
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "Api";
}
