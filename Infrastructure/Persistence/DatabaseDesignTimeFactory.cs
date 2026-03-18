using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Persistence
{
    public class DatabaseDesignTimeFactory : IDesignTimeDbContextFactory<Database>
    {
        public Database CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json")
                .Build();

            var databaseConfiguration = configuration.GetSection(DatabaseConfiguration.SectionName)
                .Get<DatabaseConfiguration>()
                ?? throw new InvalidOperationException($"Failed to load {DatabaseConfiguration.SectionName} configuration.");

            var optionsBuilder = new DbContextOptionsBuilder<Database>();
            optionsBuilder.UseNpgsql(databaseConfiguration.ConnectionString);

            return new Database(databaseConfiguration);
        }
    }
}