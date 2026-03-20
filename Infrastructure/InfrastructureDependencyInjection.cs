using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Identity;
using Musify.Infrastructure.Messaging;
using Musify.Infrastructure.Persistence;
using Musify.Infrastructure.Pictures;
using Musify.Infrastructure.Storage;

namespace Musify.Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructureModules(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddInfrastructureConfigurations(configuration)
                .AddDatabase(configuration)
                .AddKeycloakService(configuration)
                .AddStorageHandler()
                .AddPictureHandler()
                .AddMassTransitClient(configuration)
                .AddScoped<IEventBus, MassTransitEventBus>();

            return serviceDescriptors;
        }

        public static IServiceCollection AddInfrastructureWorkerModules(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddInfrastructureConfigurations(configuration)
                .AddDatabase(configuration)
                .AddStorageHandler()
                .AddPictureHandler()
                .AddMassTransitConsumers(configuration)
                .AddScoped<IEventBus, MassTransitEventBus>();

            return serviceDescriptors;
        }
    }
}
