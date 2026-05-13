using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Logs;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.Logs;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;
using Musify.Infrastructure.MassTransit.Activities.Tracks;

namespace Musify.Infrastructure.MassTransit
{
    public static class MassTransitDependencyInjection
    {
        public static IServiceCollection AddMassTransitClient(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<MassTransitConfiguration>()
                .Bind(configuration.GetRequiredSection(MassTransitConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<MassTransitConfiguration>>().Value);

            serviceDescriptors.AddScoped<IEventBus, MassTransitEventBus>();
            serviceDescriptors.AddMassTransit(options =>
            {
                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MassTransitConfiguration>()));
            } );

            return serviceDescriptors;
        }

        public static IServiceCollection AddMassTransitConsumers(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<MassTransitConfiguration>()
                .Bind(configuration.GetRequiredSection(MassTransitConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<MassTransitConfiguration>>().Value);

            serviceDescriptors
                .AddOptionsWithValidateOnStart<WorkerConfiguration>()
                .Bind(configuration.GetRequiredSection(WorkerConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<WorkerConfiguration>>().Value);

            serviceDescriptors.AddScoped<AudioWorkflowRoutingSlipBuilder>();
            serviceDescriptors.AddScoped<PictureWorkflowRoutingSlipBuilder>();
            serviceDescriptors.AddScoped<DeleteTrackRoutingSlipBuilder>();

            serviceDescriptors.AddMassTransit(options =>
            {
                options.AddConsumer<UpdatePlayListPictureConsumer>();
                options.AddConsumer<UpdateTrackPictureConsumer>();
                options.AddConsumer<RoutingSlipCleanUpConsumer>();
                options.AddConsumer<UpdateTrackAudioConsumer>();
                options.AddConsumer<DeleteTrackConsumer>();

                options.AddExecuteActivity<RemoveFileFromBucketActivity, RemoveFileFromBucketArguments>();
                options.AddExecuteActivity<GenerateAudioWorkflowPathsActivity, GenerateAudioWorkflowPathsArguments>();
                options.AddExecuteActivity<GeneratePictureWorkflowPathsActivity, GeneratePictureWorkflowPathsArguments>();
                options.AddExecuteActivity<DeleteTrackActivity, DeleteTrackArguments>();

                options.AddActivity<ResizePictureActivity, ResizePictureLocalArguments, ResizePictureLog>();
                options.AddActivity<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments, UpdatePlayListPictureLog>();
                options.AddActivity<UpdateTrackPictureActivity, UpdateTrackPictureArguments, UpdateTrackPictureLog>();
                options.AddActivity<DownloadFileFromBucketActivity, DownloadFileFromBucketArguments, DownloadFileFromBucketLog>();
                options.AddActivity<TranscodeDashAudioActivity, TranscodeDashAudioArguments, TranscodeDashAudioLog>();
                options.AddActivity<TransferFilesToBucketActivity, TransferFilesToBucketArguments, TransferFilesToBucketLog>();
                options.AddActivity<UpdateTrackAudioActivity, UpdateTrackAudioArguments, UpdateTrackAudioLog>();
                options.AddActivity<UploadFileToBucketActivity, UploadFileToBucketArguments, UploadFileToBucketLog>();

                options.UsingRabbitMq((busRegistrationContext, busFactoryConfigurator) =>
                {
                    ConfigureRabbitMqHost(
                        busFactoryConfigurator,
                        busRegistrationContext.GetRequiredService<MassTransitConfiguration>());

                    busFactoryConfigurator.ReceiveEndpoint(UpdatePlayListPictureConsumer.QueueName, endpointConfigurator =>
                        endpointConfigurator.ConfigureConsumer<UpdatePlayListPictureConsumer>(busRegistrationContext));

                    busFactoryConfigurator.ReceiveEndpoint(UpdateTrackAudioConsumer.QueueName, endpointConfigurator =>
                        endpointConfigurator.ConfigureConsumer<UpdateTrackAudioConsumer>(busRegistrationContext));

                    busFactoryConfigurator.ReceiveEndpoint(UpdateTrackPictureConsumer.QueueName, endpointConfigurator =>
                        endpointConfigurator.ConfigureConsumer<UpdateTrackPictureConsumer>(busRegistrationContext));

                    busFactoryConfigurator.ReceiveEndpoint(DeleteTrackConsumer.QueueName, endpointConfigurator =>
                        endpointConfigurator.ConfigureConsumer<DeleteTrackConsumer>(busRegistrationContext));

                    busFactoryConfigurator.ReceiveEndpoint(RoutingSlipCleanUpConsumer.QueueName, endpointConfigurator =>
                        endpointConfigurator.ConfigureConsumer<RoutingSlipCleanUpConsumer>(busRegistrationContext));

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

                    ConfigureActivityEndpoint<TransferFilesToBucketActivity, TransferFilesToBucketArguments, TransferFilesToBucketLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        TransferFilesToBucketActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<UpdateTrackAudioActivity, UpdateTrackAudioArguments, UpdateTrackAudioLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        UpdateTrackAudioActivity.ExecuteEndpointName);

                    ConfigureActivityEndpoint<UploadFileToBucketActivity, UploadFileToBucketArguments, UploadFileToBucketLog>(
                        busFactoryConfigurator,
                        busRegistrationContext,
                        UploadFileToBucketActivity.ExecuteEndpointName);
                } );
            } );

            return serviceDescriptors;
        }

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
                endpointConfigurator.ExecuteActivityHost<TActivity, TArguments>(
                    EndpointHelper.BuildCompensateActivityUri(endpointName),
                    busRegistrationContext)
            );

            busFactoryConfigurator.ReceiveEndpoint($"{endpointName}_compensate", endpointConfigurator =>
                endpointConfigurator.CompensateActivityHost<TActivity, TLog>(busRegistrationContext)
            );
        }
    }
}