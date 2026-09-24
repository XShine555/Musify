using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Sagas;
using Musify.Infrastructure.Persistence;

namespace Musify.Infrastructure.MassTransit
{
    public static partial class MassTransitDependencyInjection
    {
        public static IServiceCollection AddMassTransitClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatedOptions<MassTransitConfiguration>(configuration);
            services.AddScoped<IEventBus, MassTransitEventBus>();

            services.AddMassTransit(options =>
            {
                AddOutbox(options);

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MassTransitConfiguration>()));
            });

            return services;
        }

        public static IServiceCollection AddMassTransitConsumers(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatedOptions<MassTransitConfiguration>(configuration);

            services.AddValidatedOptions<WorkerConfiguration>(configuration);
            services.AddScoped<IEventBus, MassTransitEventBus>();

            RegisterRoutingSlipBuilders(services);

            services.AddMassTransit(options =>
            {
                AddOutbox(options);

                options.SetKebabCaseEndpointNameFormatter();

                RegisterConsumersAndActivities(options);

                options.AddSagaStateMachine<TrackProcessingStateMachine, TrackProcessingState>()
                    .EntityFrameworkRepository(ConfigureSagaRepository<TrackProcessingState>);

                options.AddSagaStateMachine<PlayListProcessingStateMachine, PlayListProcessingState>()
                    .EntityFrameworkRepository(ConfigureSagaRepository<PlayListProcessingState>);

                options.AddSagaStateMachine<AlbumProcessingStateMachine, AlbumProcessingState>()
                    .EntityFrameworkRepository(ConfigureSagaRepository<AlbumProcessingState>);

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
            });

            return services;
        }

        private static void AddOutbox(IBusRegistrationConfigurator options) =>
            options.AddEntityFrameworkOutbox<Database>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();

                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
            });

        private static void ConfigureSagaRepository<TSaga>(IEntityFrameworkSagaRepositoryConfigurator<TSaga> repository)
            where TSaga : class, ISaga
        {
            repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            repository.ExistingDbContext<Database>();
            repository.UsePostgres();
        }

        private static void UseStandardRetry(IReceiveEndpointConfigurator endpointConfigurator) =>
            endpointConfigurator.UseMessageRetry(retryConfigurator =>
                retryConfigurator.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)));

        private static void ConfigureRabbitMqHost(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            MassTransitConfiguration massTransitConfiguration)
        {
            busFactoryConfigurator.Host(massTransitConfiguration.Address, options =>
            {
                options.Username(massTransitConfiguration.Username);
                options.Password(massTransitConfiguration.Password);
            });
        }
    }
}
