using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Logs;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.Logs;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit
{
    public static partial class MassTransitDependencyInjection
    {
        static void RegisterRoutingSlipBuilders(IServiceCollection services)
        {
            services.AddScoped<AudioWorkflowRoutingSlipBuilder>();
            services.AddScoped<PictureWorkflowRoutingSlipBuilder>();
            services.AddScoped<DeleteTrackRoutingSlipBuilder>();
        }

        static void RegisterConsumersAndActivities(IBusRegistrationConfigurator options)
        {
            options.AddConsumer<UpdatePlayListPictureConsumer>();
            options.AddConsumer<UpdateTrackPictureConsumer>();
            options.AddConsumer<RoutingSlipCleanUpConsumer>();
            options.AddConsumer<UpdateTrackAudioConsumer>();
            options.AddConsumer<DeleteTrackConsumer>();
            options.AddConsumer<RemoveFileConsumer>();

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
        }

        static void ConfigureRabbitMqWorkerEndpoints(
            IBusRegistrationContext busRegistrationContext,
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator)
        {
            ConfigureRabbitMqHost(
                busFactoryConfigurator,
                busRegistrationContext.GetRequiredService<MassTransitConfiguration>());

            ConfigureConsumerEndpoint<UpdatePlayListPictureConsumer>(
                busFactoryConfigurator,
                busRegistrationContext,
                UpdatePlayListPictureConsumer.QueueName);

            ConfigureConsumerEndpoint<UpdateTrackAudioConsumer>(
                busFactoryConfigurator,
                busRegistrationContext,
                UpdateTrackAudioConsumer.QueueName);

            ConfigureConsumerEndpoint<UpdateTrackPictureConsumer>(
                busFactoryConfigurator,
                busRegistrationContext,
                UpdateTrackPictureConsumer.QueueName);

            ConfigureConsumerEndpoint<DeleteTrackConsumer>(
                busFactoryConfigurator,
                busRegistrationContext,
                DeleteTrackConsumer.QueueName);

            ConfigureConsumerEndpoint<RemoveFileConsumer>(
                busFactoryConfigurator,
                busRegistrationContext,
                RemoveFileConsumer.QueueName);

            ConfigureConsumerEndpoint<RoutingSlipCleanUpConsumer>(
                busFactoryConfigurator,
                busRegistrationContext,
                RoutingSlipCleanUpConsumer.QueueName);

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
        }
    }
}
