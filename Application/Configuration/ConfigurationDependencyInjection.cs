using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Musify.Application.Configuration
{
    public static class ConfigurationDependencyInjection
    {
        public static void AddPlayListConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var playListConfiguration = PlayListConfiguration.Load(configuration);

            serviceDescriptors.AddSingleton(playListConfiguration);
        }

        public static void AddStorageConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var storageConfiguration = StorageConfiguration.Load(configuration);

            serviceDescriptors.AddSingleton(storageConfiguration);
        }
    }
}