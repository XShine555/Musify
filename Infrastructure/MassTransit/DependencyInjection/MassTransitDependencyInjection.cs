using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.MassTransit
{
    public static partial class MassTransitDependencyInjection
    {
        public static IServiceCollection AddMassTransitClient(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            RegisterValidatedOptions<MassTransitConfiguration>(
                serviceDescriptors,
                configuration,
                MassTransitConfiguration.SectionName);

            serviceDescriptors.AddScoped<IEventBus, MassTransitEventBus>();
            serviceDescriptors.AddMassTransit(options =>
            {
                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MassTransitConfiguration>()));
            } );

            return serviceDescriptors;
        }

        public static IServiceCollection AddMassTransitConsumers(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            RegisterValidatedOptions<MassTransitConfiguration>(
                serviceDescriptors,
                configuration,
                MassTransitConfiguration.SectionName);

            RegisterValidatedOptions<WorkerConfiguration>(
                serviceDescriptors,
                configuration,
                WorkerConfiguration.SectionName);

            RegisterRoutingSlipBuilders(serviceDescriptors);

            serviceDescriptors.AddMassTransit(options =>
            {
                RegisterConsumersAndActivities(options);

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                    ConfigureRabbitMqWorkerEndpoints(busRegistrationContext, busFactoryConfigurator));
            } );

            return serviceDescriptors;
        }

        static void ConfigureRabbitMqHost(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            MassTransitConfiguration massTransitConfiguration)
        {
            busFactoryConfigurator.Host(massTransitConfiguration.Address, options =>
            {
                options.Username(massTransitConfiguration.Username);
                options.Password(massTransitConfiguration.Password);
            } );
        }
    }
}