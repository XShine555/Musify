using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Musify.Application.Configuration
{
    public static class ConfigurationDependencyInjection
    {
        public static IServiceCollection AddApplicationStorageConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<ApplicationStorageConfiguration>()
                .Bind(configuration.GetRequiredSection(ApplicationStorageConfiguration.SectionName))
                .ValidateDataAnnotations();
            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<ApplicationStorageConfiguration>>().Value);

            return serviceDescriptors;
        }

        public static IServiceCollection AddPlayListConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<PlayListConfiguration>()
                .Bind(configuration.GetRequiredSection(PlayListConfiguration.SectionName))
                .ValidateDataAnnotations();
            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<PlayListConfiguration>>().Value);

            return serviceDescriptors;
        }

        public static IServiceCollection AddTrackConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<TrackConfiguration>()
                .Bind(configuration.GetRequiredSection(TrackConfiguration.SectionName))
                .ValidateDataAnnotations();
            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<TrackConfiguration>>().Value);

            return serviceDescriptors;
        }

        public static IServiceCollection AddStreamGatewayConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<StreamGatewayConfiguration>()
                .Bind(configuration.GetRequiredSection(StreamGatewayConfiguration.SectionName))
                .ValidateDataAnnotations();
            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<StreamGatewayConfiguration>>().Value);

            return serviceDescriptors;
        }
    }
}