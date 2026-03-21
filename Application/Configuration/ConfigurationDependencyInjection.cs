using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Musify.Application.Configuration
{
    public static class ConfigurationDependencyInjection
    {
        public static IServiceCollection AddApplicationConfigurations(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddValidatedConfiguration<ApplicationStorageConfiguration>(configuration, ApplicationStorageConfiguration.SectionName)
                .AddValidatedConfiguration<PlayListConfiguration>(configuration, PlayListConfiguration.SectionName)
                .AddValidatedConfiguration<TrackConfiguration>(configuration, TrackConfiguration.SectionName);

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
