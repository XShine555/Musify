using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Consumers;

namespace Musify.Infrastructure.Messaging
{
    public static class MassTransitDependencyInjection
    {
        public static IServiceCollection AddMassTransitClient(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddMassTransit(options =>
            {
                options.UsingRabbitMq((_, busFactoryConfigurator) =>
                    ConfigureRabbitMqHost(busFactoryConfigurator, configuration));
            } );

            return serviceDescriptors;
        }

        public static IServiceCollection AddMassTransitConsumers(this IServiceCollection serviceDescriptors, IConfiguration configuration)
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
                    ConfigureRabbitMqHost(busFactoryConfigurator, configuration);

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

        static void ConfigureRabbitMqHost(IRabbitMqBusFactoryConfigurator busFactoryConfigurator, IConfiguration configuration)
        {
            var section = configuration.GetSection("MassTransit");

            var host = section["Host"]
                ?? throw new InvalidOperationException("MassTransit:Host configuration value not found.");

            var username = section["Username"]
                ?? throw new InvalidOperationException("MassTransit:Username configuration value not found.");

            var password = section["Password"]
                ?? throw new InvalidOperationException("MassTransit:Password configuration value not found.");

            busFactoryConfigurator.Host(host, options =>
            {
                options.Username(username);
                options.Password(password);
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