using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Persistence;

public class DatabaseDesignTimeFactory : IDesignTimeDbContextFactory<Database>
{
    public Database CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("DesignSettings.json", optional: true)
            .AddUserSecrets<DatabaseDesignTimeFactory>()
            .AddEnvironmentVariables()
            .Build();

        var databaseConfiguration = configuration
            .GetRequiredSection(DatabaseConfiguration.SectionName)
            .Get<DatabaseConfiguration>()
            ?? throw new InvalidOperationException($"{DatabaseConfiguration.SectionName} configuration section not found.");

        return new Database(databaseConfiguration);
    }
}
