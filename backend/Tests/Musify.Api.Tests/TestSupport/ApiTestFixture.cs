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

/// <summary>
/// Boots the real <c>Musify.Api</c> host — real routing, real auth/authorization pipeline,
/// real validators, real Mediator handlers — against a private Postgres container (so EF
/// queries behave like production, not like a stand-in). The services this API talks to over
/// the network (object storage, YouTube, the message bus, stream-ticket signing) are replaced
/// with NSubstitute fakes: they are Infrastructure's job to verify against the real thing (see
/// Musify.Infrastructure.Tests), not the HTTP layer's.
/// </summary>
public sealed class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("musify_api_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IStorageService StorageService { get; } = Substitute.For<IStorageService>();

    public IEventBus EventBus { get; } = Substitute.For<IEventBus>();

    public IYouTubeMusicService YouTubeMusicService { get; } = Substitute.For<IYouTubeMusicService>();

    public IStreamTicketService StreamTicketService { get; } = Substitute.For<IStreamTicketService>();

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();

        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Musify.Infrastructure.Persistence.Database>();
        await database.Database.MigrateAsync();
    }

    // xUnit v3's IAsyncLifetime IS System.IAsyncDisposable (rather than declaring its own
    // Task-returning DisposeAsync as in v2), so there is a single DisposeAsync slot to fill —
    // override the one WebApplicationFactory already provides instead of implementing the
    // interface explicitly alongside it.
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await postgres.DisposeAsync();
    }

    /// <summary>An <see cref="HttpClient"/> with no auth header — every protected endpoint answers 401.</summary>
    public HttpClient CreateAnonymousClient() => CreateClient();

    /// <summary>An <see cref="HttpClient"/> authenticated as the given user id (see <see cref="FakeAuthenticationHandler"/>).</summary>
    public HttpClient CreateAuthenticatedClient(long userId, string userName = "test-user")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(FakeAuthenticationHandler.UserIdHeader, userId.ToString());
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
                ["PlayList:SeedPresetPictures"] = "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton(StorageService));
            services.Replace(ServiceDescriptor.Scoped(_ => EventBus));
            services.Replace(ServiceDescriptor.Singleton(YouTubeMusicService));
            services.Replace(ServiceDescriptor.Singleton(StreamTicketService));

            // Overrides the real JwtBearer scheme as the default, so requests authenticate via
            // FakeAuthenticationHandler instead of needing a real token and a reachable IdP.
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
