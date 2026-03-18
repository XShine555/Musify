using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Musify.Infrastructure.Configuration
{
    public static class ConfigurationDependencyInjection
    {
        public static void AddKeycloakConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var keycloakConfiguration = KeycloakConfiguration.Load(configuration);
            serviceDescriptors.AddSingleton(keycloakConfiguration);
        }

        public static void AddDatabaseConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var databaseConfiguration = DatabaseConfiguration.Load(configuration);
            serviceDescriptors.AddSingleton(databaseConfiguration);
        }
    }
}