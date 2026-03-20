using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Musify.Infrastructure.Configuration
{
    public static class ConfigurationDependencyInjection
    {
        public static IServiceCollection AddInfrastructureConfigurations(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddValidatedConfiguration<DatabaseConfiguration>(configuration, DatabaseConfiguration.SectionName)
                .AddValidatedConfiguration<KeycloakConfiguration>(configuration, KeycloakConfiguration.SectionName)
                .AddValidatedConfiguration<StorageClientConfiguration>(configuration, StorageClientConfiguration.SectionName);

            return serviceDescriptors;
        }

        static IServiceCollection AddValidatedConfiguration<TConfiguration>(
            this IServiceCollection serviceDescriptors,
            IConfiguration configuration,
            string sectionName)
            where TConfiguration : class
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<TConfiguration>()
                .Bind(configuration.GetRequiredSection(sectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<TConfiguration>>().Value);

            return serviceDescriptors;
        }
    }
}
