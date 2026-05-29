using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Musify.Application.Contracts;
using Musify.Application.Tests.Infrastructure;
using NSubstitute;

namespace Musify.WebApi.Tests.Integration;

public sealed class MusifyApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:;Foreign Keys=False");

    public IStorageService Storage { get; } = Substitute.For<IStorageService>();
    public IEventBus EventBus { get; } = Substitute.For<IEventBus>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["ApplicationStorage:Bucket"] = TestData.Bucket,
                ["PlayList:Routes:ParentFolders"] = "PlayLists",
                ["Track:Routes:ParentFolder"] = "Tracks",
                ["UploadIntent:TempRootPrefix"] = "temp",
                ["InfrastructureStorage:Address"] = "http://localhost:9000",
                ["InfrastructureStorage:AccessKey"] = "test",
                ["InfrastructureStorage:SecretAccessKey"] = "test",
                ["InfrastructureStorage:ForcePathStyle"] = "true",
                ["InfrastructureStorage:UseHttp"] = "true",
                ["MassTransit:Address"] = "rabbitmq://localhost",
                ["MassTransit:Username"] = "guest",
                ["MassTransit:Password"] = "guest",
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317",
            } );
        } );

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            _connection.Open();

            services.RemoveAll<IDatabase>();
            services.AddScoped<IDatabase>(_ =>
            {
                var options = new DbContextOptionsBuilder<TestDatabase>()
                    .UseSqlite(_connection)
                    .Options;
                var db = new TestDatabase(options);
                db.Database.EnsureCreated();
                return db;
            });

            services.RemoveAll<IStorageService>();
            services.AddScoped(_ => Storage);

            services.RemoveAll<IEventBus>();
            services.AddScoped(_ => EventBus);
        } );
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
