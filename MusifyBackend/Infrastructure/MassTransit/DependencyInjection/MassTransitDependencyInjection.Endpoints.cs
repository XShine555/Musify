using MassTransit;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.Persistence;

namespace Musify.Infrastructure.MassTransit
{
    public static partial class MassTransitDependencyInjection
    {
        static void ConfigureConsumerEndpoint<TConsumer>(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            IBusRegistrationContext busRegistrationContext,
            string queueName)
            where TConsumer : class, IConsumer
        {
            busFactoryConfigurator.ReceiveEndpoint(queueName, endpointConfigurator =>
            {
                endpointConfigurator.UseMessageRetry(retryConfigurator =>
                    retryConfigurator.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(2)));

                endpointConfigurator.UseEntityFrameworkOutbox<Database>(busRegistrationContext);

                endpointConfigurator.ConfigureConsumer<TConsumer>(busRegistrationContext);
            });
        }

        static void ConfigureExecuteActivityEndpoint<TActivity, TArguments>(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            IBusRegistrationContext busRegistrationContext,
            string endpointName)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            busFactoryConfigurator.ReceiveEndpoint(MassTransitEndpointNames.ExecuteQueue(endpointName), endpointConfigurator =>
            {
                endpointConfigurator.UseMessageRetry(retryConfigurator =>
                    retryConfigurator.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(2)));

                endpointConfigurator.UseEntityFrameworkOutbox<Database>(busRegistrationContext);

                endpointConfigurator.ExecuteActivityHost<TActivity, TArguments>(busRegistrationContext);
            });
        }

        static void ConfigureActivityEndpoint<TActivity, TArguments, TLog>(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            IBusRegistrationContext busRegistrationContext,
            string endpointName)
            where TActivity : class, IActivity<TArguments, TLog>
            where TArguments : class
            where TLog : class
        {
            busFactoryConfigurator.ReceiveEndpoint(MassTransitEndpointNames.ExecuteQueue(endpointName), endpointConfigurator =>
            {
                endpointConfigurator.UseMessageRetry(retryConfigurator =>
                    retryConfigurator.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(2)));

                endpointConfigurator.UseEntityFrameworkOutbox<Database>(busRegistrationContext);

                endpointConfigurator.ExecuteActivityHost<TActivity, TArguments>(
                    EndpointHelper.BuildCompensateActivityUri(endpointName),
                    busRegistrationContext);
            });

            busFactoryConfigurator.ReceiveEndpoint(MassTransitEndpointNames.CompensateQueue(endpointName), endpointConfigurator =>
            {
                endpointConfigurator.UseMessageRetry(retryConfigurator =>
                    retryConfigurator.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(2)));

                endpointConfigurator.UseEntityFrameworkOutbox<Database>(busRegistrationContext);

                endpointConfigurator.CompensateActivityHost<TActivity, TLog>(busRegistrationContext);
            });
        }
    }
}
