using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Messaging.Consumers;

namespace Musify.Infrastructure.Messaging
{
    public static class MassTransitDependencyInjection
    {
        public static void AddMassTransitHandler(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddMassTransit(options =>
            {
                options.AddConsumer<PictureResizerConsumer>();

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                {
                    var section = configuration.GetSection("MassTransit");

                    var host = section["Host"]
                        ?? throw new InvalidOperationException("MassTransit:Host configuration is missing.");

                    busFactoryConfigurator.Host(host);

                    busFactoryConfigurator.ReceiveEndpoint(PictureResizerConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<PictureResizerConsumer>(busRegistrationContext);
                    } );
                } );
            } );
        }
    }
}