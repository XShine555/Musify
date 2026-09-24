using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.MassTransit.Activities.Albums;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.LifeCycle;
using Musify.Infrastructure.MassTransit.Activities.Pictures;
using Musify.Infrastructure.MassTransit.Activities.PlayLists;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Activities.UploadIntents;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.Logs;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit
{
    public static partial class MassTransitDependencyInjection
    {
        private static void RegisterRoutingSlipBuilders(IServiceCollection services)
        {
            services.AddScoped<AudioWorkflowRoutingSlipBuilder>();
            services.AddScoped<CreateTrackRoutingSlipBuilder>();
            services.AddScoped<DeleteRoutingSlipBuilder>();
            services.AddScoped<PictureSourceRoutingSlipBuilder>();
            services.AddScoped<PictureWorkflowRoutingSlipBuilder>();
        }

        private static void RegisterConsumersAndActivities(IBusRegistrationConfigurator options)
        {
            options.AddConsumer<AlbumProcessingFailedConsumer>();
            options.AddConsumer<CreateAlbumConsumer>();
            options.AddConsumer<CreatePlayListConsumer>();
            options.AddConsumer<CreateTrackConsumer>();
            options.AddConsumer<DeleteAlbumConsumer>();
            options.AddConsumer<DeletePlayListConsumer>();
            options.AddConsumer<DeleteTrackConsumer>();
            options.AddConsumer<PlayListProcessingFailedConsumer>();
            options.AddConsumer<ProcessingSlipFaultConsumer>();
            options.AddConsumer<RoutingSlipCleanUpConsumer>();
            options.AddConsumer<TrackProcessingFailedConsumer>();
            options.AddConsumer<UpdateAlbumPictureConsumer>();
            options.AddConsumer<UpdateAlbumPictureSourceConsumer>();
            options.AddConsumer<UpdatePlayListPictureConsumer>();
            options.AddConsumer<UpdatePlayListPictureSourceConsumer>();
            options.AddConsumer<UpdateTrackAudioConsumer>();
            options.AddConsumer<UpdateTrackPictureConsumer>();

            options.AddExecuteActivity<DeleteAlbumActivity, DeleteEntityArguments>();
            options.AddExecuteActivity<DeletePlayListActivity, DeleteEntityArguments>();
            options.AddExecuteActivity<DeleteTrackActivity, DeleteEntityArguments>();
            options.AddExecuteActivity<GenerateAudioWorkflowPathsActivity, GenerateAudioWorkflowPathsArguments>();
            options.AddExecuteActivity<GeneratePictureWorkflowPathsActivity, GeneratePictureWorkflowPathsArguments>();
            options.AddExecuteActivity<MarkAlbumLifeCycleActivity, MarkLifeCycleArguments>();
            options.AddExecuteActivity<MarkPlayListLifeCycleActivity, MarkLifeCycleArguments>();
            options.AddExecuteActivity<MarkTrackLifeCycleActivity, MarkLifeCycleArguments>();
            options.AddExecuteActivity<PublishAlbumPictureProcessingEventActivity, PublishAlbumPictureProcessingEventArguments>();
            options.AddExecuteActivity<PublishPlayListPictureProcessingEventActivity, PublishPlayListPictureProcessingEventArguments>();
            options.AddExecuteActivity<PublishTrackProcessingEventsActivity, PublishTrackProcessingEventsArguments>();
            options.AddExecuteActivity<RemoveFileFromBucketActivity, RemoveFileFromBucketArguments>();

            options.AddActivity<ConsumeUploadIntentsActivity, ConsumeUploadIntentsArguments, ConsumeUploadIntentsLog>();
            options.AddActivity<CopyFileInBucketActivity, CopyFileInBucketArguments, CopyFileInBucketLog>();
            options.AddActivity<DownloadFileFromBucketActivity, DownloadFileFromBucketArguments, DownloadFileFromBucketLog>();
            options.AddActivity<ResizePictureActivity, ResizePictureLocalArguments, ResizePictureLog>();
            options.AddActivity<TranscodeAudioActivity, TranscodeAudioArguments, TranscodeAudioLog>();
            options.AddActivity<TransferFilesToBucketActivity, TransferFilesToBucketArguments, TransferFilesToBucketLog>();
            options.AddActivity<UpdateAlbumPictureActivity, UpdatePicturesArguments, UpdatePicturesLog>();
            options.AddActivity<UpdatePlayListPictureActivity, UpdatePicturesArguments, UpdatePicturesLog>();
            options.AddActivity<UpdateTrackAudioActivity, UpdateTrackAudioArguments, UpdateTrackAudioLog>();
            options.AddActivity<UpdateTrackPictureActivity, UpdatePicturesArguments, UpdatePicturesLog>();
            options.AddActivity<UploadFileToBucketActivity, UploadFileToBucketArguments, UploadFileToBucketLog>();
        }
    }
}
