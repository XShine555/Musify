using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Sagas;
using Musify.Infrastructure.Persistence;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.MassTransit;

public static partial class MassTransitDependencyInjection
{
    public static IServiceCollection AddMassTransitClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<MassTransitConfiguration>(configuration);

        services.AddScoped<IEventBus, MassTransitEventBus>();
        services.AddMassTransit(options =>
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
        });

        return services;
    }

    public static IServiceCollection AddMassTransitConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<MassTransitConfiguration>(configuration);

        services.AddValidatedOptions<WorkerConfiguration>(configuration);

        RegisterRoutingSlipBuilders(services);

        services.AddMassTransit(options =>
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

            options.AddSagaStateMachine<PlayListProcessingStateMachine, PlayListProcessingState>()
                .EntityFrameworkRepository(repository =>
                {
                    repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                    repository.ExistingDbContext<Database>();
                    repository.UsePostgres();
                });

            options.AddSagaStateMachine<AlbumProcessingStateMachine, AlbumProcessingState>()
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
        });

        return services;
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
