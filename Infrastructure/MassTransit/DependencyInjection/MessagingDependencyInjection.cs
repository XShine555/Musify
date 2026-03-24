using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Activities.Logs;
using Musify.Infrastructure.Messaging.Consumers;
using Musify.Infrastructure.Messaging.Filters;
using Musify.Infrastructure.Messaging.RoutingSlip.Builders;

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
            serviceDescriptors.AddScoped<AudioWorkflowRoutingSlipBuilder>();
            serviceDescriptors.AddScoped<PictureWorkflowRoutingSlipBuilder>();
            serviceDescriptors.AddScoped<RemoveFileRoutingSlipBuilder>();

            serviceDescriptors.AddMassTransit(options =>
            {
                options.AddConsumer<UpdatePlayListPictureConsumer>();
                options.AddConsumer<RemoveFileConsumer>();
                options.AddConsumer<TranscodeAudioFromTrackConsumer>();
                options.AddConsumer<UpdateTrackPictureConsumer>();

                options.AddExecuteActivity<RemoveFileFromBucketActivity, RemoveFileFromBucketArguments>();
                options.AddExecuteActivity<GenerateAudioWorkflowPathsActivity, GenerateAudioWorkflowPathsArguments>();
                options.AddExecuteActivity<GeneratePictureWorkflowPathsActivity, GeneratePictureWorkflowPathsArguments>();

                options.AddActivity<ResizePictureActivity, ResizePictureLocalArguments, ResizePictureLog>();
                options.AddActivity<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments, UpdatePlayListPictureLog>();
                options.AddActivity<UpdateTrackPictureActivity, UpdateTrackPictureArguments, UpdateTrackPictureLog>();
                options.AddActivity<DownloadFileFromBucketActivity, DownloadFileFromBucketArguments, DownloadFileFromBucketLog>();
                options.AddActivity<TranscodeDashAudioActivity, TranscodeDashAudioArguments, TranscodeDashAudioLog>();
                options.AddActivity<TransferFilesToBucket, TransferFilesToBucketArguments, TransferFilesToBucketLog>();

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

                    busFactoryConfigurator.ReceiveEndpoint(TranscodeAudioFromTrackConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<TranscodeAudioFromTrackConsumer>(busRegistrationContext);
                    } );

                    busFactoryConfigurator.ReceiveEndpoint(UpdateTrackPictureConsumer.QueueName, endpointConfigurator =>
                    {
                        endpointConfigurator.ConfigureConsumer<UpdateTrackPictureConsumer>(busRegistrationContext);
                    } );

                    ConfigureExecuteActivityEndpoint<RemoveFileFromBucketActivity, RemoveFileFromBucketArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        RemoveFileFromBucketActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<GenerateAudioWorkflowPathsActivity, GenerateAudioWorkflowPathsArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        GenerateAudioWorkflowPathsActivity.ExecuteEndpointName);

                    ConfigureExecuteActivityEndpoint<GeneratePictureWorkflowPathsActivity, GeneratePictureWorkflowPathsArguments>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        GeneratePictureWorkflowPathsActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<ResizePictureActivity, ResizePictureLocalArguments, ResizePictureLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        ResizePictureActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments, UpdatePlayListPictureLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        UpdatePlayListPictureActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<UpdateTrackPictureActivity, UpdateTrackPictureArguments, UpdateTrackPictureLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        UpdateTrackPictureActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<DownloadFileFromBucketActivity, DownloadFileFromBucketArguments, DownloadFileFromBucketLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        DownloadFileFromBucketActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<TranscodeDashAudioActivity, TranscodeDashAudioArguments, TranscodeDashAudioLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        TranscodeDashAudioActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<TransferFilesToBucket, TransferFilesToBucketArguments, TransferFilesToBucketLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        TransferFilesToBucket.ExecuteEndpointName);
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

        static void ConfigureActivityEndpoint<TActivity, TArguments, TLog>(
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
            } );

            busFactoryConfigurator.ReceiveEndpoint($"{endpointName}_compensate", endpointConfigurator =>
            {
                endpointConfigurator.CompensateActivityHost<TActivity, TLog>(busRegistrationContext);
            } );
        }
    }
}