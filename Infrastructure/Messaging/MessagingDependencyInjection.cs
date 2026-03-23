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
            });

            return serviceDescriptors;
        }

        public static IServiceCollection AddMassTransitConsumers(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddMassTransit(options =>
            {
                // Register consumers
                options.AddConsumer<UpdatePlayListPictureConsumer>();
                options.AddConsumer<RemoveFileConsumer>();
                options.AddConsumer<TranscodeAudioFromTrackConsumer>();
                options.AddConsumer<UpdateTrackPictureConsumer>();

                // Register execute activities
                options.AddExecuteActivity<ResizePictureActivity, ResizePictureLocalArguments>();
                options.AddExecuteActivity<RemoveFileFromBucketActivity, RemoveFileFromBucketArguments>();
                options.AddExecuteActivity<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments>();
                options.AddExecuteActivity<UpdateTrackPictureActivity, UpdateTrackPictureArguments>();
                options.AddExecuteActivity<DownloadFileFromBucketActivity, DownloadFileFromBucketArguments>();
                options.AddExecuteActivity<TranscodeDashAudioActivity, TranscodeDashAudioArguments>();
                options.AddExecuteActivity<TransferFilesToBucket, TransferFilesToBucketArguments>();

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                {
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MessagingConfiguration>());

                    busFactoryConfigurator.UseConsumeFilter(typeof(ProcessTrackingConsumeFilter<>), busRegistrationContext);

                    // Consumer endpoints
                    busFactoryConfigurator.ReceiveEndpoint(UpdatePlayListPictureConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<UpdatePlayListPictureConsumer>(busRegistrationContext);
                    });

                    busFactoryConfigurator.ReceiveEndpoint(RemoveFileConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<RemoveFileConsumer>(busRegistrationContext);
                    });

                    busFactoryConfigurator.ReceiveEndpoint(TranscodeAudioFromTrackConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<TranscodeAudioFromTrackConsumer>(busRegistrationContext);
                    });

                    busFactoryConfigurator.ReceiveEndpoint(UpdateTrackPictureConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<UpdateTrackPictureConsumer>(busRegistrationContext);
                    });

                    // Activity endpoints
                    ConfigureExecuteActivityEndpoint<ResizePictureActivity, ResizePictureLocalArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        ResizePictureActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<RemoveFileFromBucketActivity, RemoveFileFromBucketArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        RemoveFileFromBucketActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        UpdatePlayListPictureActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<UpdateTrackPictureActivity, UpdateTrackPictureArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        UpdateTrackPictureActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<DownloadFileFromBucketActivity, DownloadFileFromBucketArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        DownloadFileFromBucketActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<TranscodeDashAudioActivity, TranscodeDashAudioArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        TranscodeDashAudioActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<TransferFilesToBucket, TransferFilesToBucketArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        TransferFilesToBucket.ExecuteEndpointName);
                });
            });

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
            });
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
            });
        }

        static void ConfigureActivityEndpoints<TActivity, TArguments, TLog>(
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            IBusRegistrationContext busRegistrationContext,
            string endpointName)
            where TActivity : class, IActivity<TArguments, TLog>
            where TArguments : class
            where TLog : class
        {
            busFactoryConfigurator.ReceiveEndpoint($"{endpointName}_execute", endpointConfigurator =>
            {
                endpointConfigurator.ExecuteActivityHost<TActivity, TArguments>(busRegistrationContext);
            });

            busFactoryConfigurator.ReceiveEndpoint($"{endpointName}_compensate", endpointConfigurator =>
            {
                endpointConfigurator.CompensateActivityHost<TActivity, TLog>(busRegistrationContext);
            });
        }
    }
}