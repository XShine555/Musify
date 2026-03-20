using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Messaging.Consumers;

namespace Musify.Infrastructure.Messaging
{
    public static class MassTransitDependencyInjection
    {
        public static IServiceCollection AddMassTransitClient(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddMassTransit(options =>
            {
                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
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
                } );
            } );
            return serviceDescriptors;
        }

        public static IServiceCollection AddMassTransitConsumers(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddMassTransit(options =>
            {
                options.AddConsumer<UpdatePlayListPictureConsumer>();

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
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

                    busFactoryConfigurator.ReceiveEndpoint(UpdatePlayListPictureConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<UpdatePlayListPictureConsumer>(busRegistrationContext);
                    } );
                } );
            } );

            return serviceDescriptors;
        }
    }
}