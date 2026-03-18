using Microsoft.Extensions.Configuration;

namespace Musify.Infrastructure.Configuration
{
    public class DatabaseConfiguration
    {
        public const string SectionName = "Database";

        public string ConnectionString { get; set; } = string.Empty;

        public static DatabaseConfiguration Load(IConfiguration configuration)
        {
            var databaseConfiguration = configuration.GetSection(SectionName).Get<DatabaseConfiguration>()
                ?? throw new InvalidOperationException($"{SectionName} configuration section not found.");

            Validate(databaseConfiguration);
            return databaseConfiguration;
        }

        static void Validate(DatabaseConfiguration configuration)
        {
            EnsureValue(configuration.ConnectionString, nameof(configuration.ConnectionString));
        }

        static void EnsureValue(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"{name} configuration value not found.");
        }
    }
}