using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Sagas;
using Musify.Infrastructure.Persistence;

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
                options.AddEntityFrameworkOutbox<Database>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.UseBusOutbox();

                    outbox.QueryDelay = TimeSpan.FromSeconds(1);
                    outbox.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
                });

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
                options.AddEntityFrameworkOutbox<Database>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.UseBusOutbox();

                    outbox.QueryDelay = TimeSpan.FromSeconds(1);
                    outbox.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
                });

                options.SetKebabCaseEndpointNameFormatter();

                RegisterConsumersAndActivities(options);

                options.AddSagaStateMachine<TrackProcessingStateMachine, TrackProcessingState>()
                    .EntityFrameworkRepository(repository =>
                    {
                        repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                        repository.ExistingDbContext<Database>();
                        repository.UsePostgres();
                    });

                options.AddConfigureEndpointsCallback((registrationContext, _, endpointConfigurator) =>
                {
                    UseStandardRetry(endpointConfigurator);
                    endpointConfigurator.UseEntityFrameworkOutbox<Database>(registrationContext);
                });

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                {
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MassTransitConfiguration>());

                    busFactoryConfigurator.ConfigureEndpoints(busRegistrationContext);
                });
            } );

            return serviceDescriptors;
        }

        static void UseStandardRetry(IReceiveEndpointConfigurator endpointConfigurator) =>
            endpointConfigurator.UseMessageRetry(retryConfigurator =>
                retryConfigurator.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)));

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