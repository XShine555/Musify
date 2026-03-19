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
                .AddJsonFile("DesignSettings.json")
                .Build();
            var databaseConfiguration = DatabaseConfiguration.Load(configuration);

            var optionsBuilder = new DbContextOptionsBuilder<Database>();
            optionsBuilder.UseNpgsql(databaseConfiguration.ConnectionString);

            return new Database(databaseConfiguration);
        }
    }
}