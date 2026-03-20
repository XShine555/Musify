using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Consumers;
using Musify.Infrastructure.Messaging.Filters;

namespace Musify.Infrastructure.Messaging
{
    public static class MessagingDependencyInjection
    {
        public static IServiceCollection AddMassTransitClient(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddMassTransit(options =>
            {
                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MessagingConfiguration>()));
            } );

            return serviceDescriptors;
        }

        public static IServiceCollection AddMassTransitConsumers(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddMassTransit(options =>
            {
                options.AddConsumer<UpdatePlayListPictureConsumer>();
                options.AddConsumer<RemoveFileConsumer>();
                options.AddExecuteActivity<ResizePictureActivity, ResizePictureArgument>();
                options.AddExecuteActivity<RemoveFilesActivity, RemoveFileArguments>();
                options.AddExecuteActivity<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments>();

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                {
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MessagingConfiguration>());

                    busFactoryConfigurator.UseConsumeFilter(typeof(ProcessTrackingConsumeFilter<>), busRegistrationContext);

                    busFactoryConfigurator.ReceiveEndpoint(UpdatePlayListPictureConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<UpdatePlayListPictureConsumer>(busRegistrationContext);
                    } );

                    busFactoryConfigurator.ReceiveEndpoint(RemoveFileConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<RemoveFileConsumer>(busRegistrationContext);
                    } );

                    ConfigureExecuteActivityEndpoint<ResizePictureActivity, ResizePictureArgument>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        ResizePictureActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<RemoveFilesActivity, RemoveFileArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        RemoveFilesActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        UpdatePlayListPictureActivity.ExecuteEndpointName);
                } );
            } );

            return serviceDescriptors;
        }

        static void ConfigureRabbitMqHost(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            MessagingConfiguration massTransitConfiguration)
        {
            busFactoryConfigurator.Host(massTransitConfiguration.Host, options =>
            {
                options.Username(massTransitConfiguration.Username);
                options.Password(massTransitConfiguration.Password);
            } );
        }

        static void ConfigureExecuteActivityEndpoint<TActivity, TArguments>(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            IBusRegistrationContext busRegistrationContext,
            string endpointName)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            busFactoryConfigurator.ReceiveEndpoint($"{endpointName}_execute", endpointConfigurator =>
            {
                endpointConfigurator.ExecuteActivityHost<TActivity, TArguments>(busRegistrationContext);
            } );
        }
    }
}