using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Musify.Infrastructure.MassTransit.Activities.Albums;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Pictures;
using Musify.Infrastructure.MassTransit.Activities.PlayLists;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Activities.UploadIntents;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.Logs;
using Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

namespace Musify.Infrastructure.MassTransit;

public static partial class MassTransitDependencyInjection
{
    private static void RegisterRoutingSlipBuilders(IServiceCollection services)
    {
        services.AddScoped<AudioWorkflowRoutingSlipBuilder>();
        services.AddScoped<PictureWorkflowRoutingSlipBuilder>();
        services.AddScoped<DeleteTrackRoutingSlipBuilder>();
        services.AddScoped<DeletePlayListRoutingSlipBuilder>();
        services.AddScoped<DeleteAlbumRoutingSlipBuilder>();
        services.AddScoped<CreateTrackRoutingSlipBuilder>();
        services.AddScoped<PlayListPictureSourceRoutingSlipBuilder>();
        services.AddScoped<AlbumPictureSourceRoutingSlipBuilder>();
    }

    private static void RegisterConsumersAndActivities(IBusRegistrationConfigurator options)
    {
        options.AddConsumer<UpdatePlayListPictureConsumer>();
        options.AddConsumer<UpdateTrackPictureConsumer>();
        options.AddConsumer<RoutingSlipCleanUpConsumer>();
        options.AddConsumer<UpdateTrackAudioConsumer>();
        options.AddConsumer<DeleteTrackConsumer>();
        options.AddConsumer<DeletePlayListConsumer>();
        options.AddConsumer<DeleteAlbumConsumer>();
        options.AddConsumer<CreateTrackConsumer>();
        options.AddConsumer<CreatePlayListConsumer>();
        options.AddConsumer<UpdatePlayListPictureSourceConsumer>();
        options.AddConsumer<ProcessingSlipFaultConsumer>();
        options.AddConsumer<TrackProcessingFailedConsumer>();
        options.AddConsumer<PlayListProcessingFailedConsumer>();
        options.AddConsumer<CreateAlbumConsumer>();
        options.AddConsumer<UpdateAlbumPictureSourceConsumer>();
        options.AddConsumer<UpdateAlbumPictureConsumer>();
        options.AddConsumer<AlbumProcessingFailedConsumer>();

        options.AddExecuteActivity<RemoveFileFromBucketActivity, RemoveFileFromBucketArguments>();
        options.AddExecuteActivity<GenerateAudioWorkflowPathsActivity, GenerateAudioWorkflowPathsArguments>();
        options.AddExecuteActivity<GeneratePictureWorkflowPathsActivity, GeneratePictureWorkflowPathsArguments>();
        options.AddExecuteActivity<MarkTrackAsRemovingActivity, MarkTrackAsRemovingArguments>();
        options.AddExecuteActivity<DeleteTrackFromDbActivity, DeleteTrackFromDbArguments>();
        options.AddExecuteActivity<MarkPlayListAsRemovingActivity, MarkPlayListAsRemovingArguments>();
        options.AddExecuteActivity<DeletePlayListFromDbActivity, DeletePlayListFromDbArguments>();
        options.AddExecuteActivity<PublishTrackProcessingEventsActivity, PublishTrackProcessingEventsArguments>();
        options.AddExecuteActivity<PublishPlayListPictureProcessingEventActivity, PublishPlayListPictureProcessingEventArguments>();
        options.AddExecuteActivity<MarkTrackAsFailedActivity, MarkTrackAsFailedArguments>();
        options.AddExecuteActivity<MarkPlayListAsFailedActivity, MarkPlayListAsFailedArguments>();
        options.AddExecuteActivity<MarkAlbumAsFailedActivity, MarkAlbumAsFailedArguments>();
        options.AddExecuteActivity<MarkAlbumAsRemovingActivity, MarkAlbumAsRemovingArguments>();
        options.AddExecuteActivity<DeleteAlbumFromDbActivity, DeleteAlbumFromDbArguments>();
        options.AddExecuteActivity<PublishAlbumPictureProcessingEventActivity, PublishAlbumPictureProcessingEventArguments>();

        options.AddActivity<ResizePictureActivity, ResizePictureLocalArguments, ResizePictureLog>();
        options.AddActivity<UpdatePlayListPictureActivity, UpdatePlayListPictureArguments, UpdatePlayListPictureLog>();
        options.AddActivity<UpdateAlbumPictureActivity, UpdateAlbumPictureArguments, UpdateAlbumPictureLog>();
        options.AddActivity<UpdateTrackPictureActivity, UpdateTrackPictureArguments, UpdateTrackPictureLog>();
        options.AddActivity<DownloadFileFromBucketActivity, DownloadFileFromBucketArguments, DownloadFileFromBucketLog>();
        options.AddActivity<TranscodeAudioActivity, TranscodeAudioArguments, TranscodeAudioLog>();
        options.AddActivity<TransferFilesToBucketActivity, TransferFilesToBucketArguments, TransferFilesToBucketLog>();
        options.AddActivity<UpdateTrackAudioActivity, UpdateTrackAudioArguments, UpdateTrackAudioLog>();
        options.AddActivity<UploadFileToBucketActivity, UploadFileToBucketArguments, UploadFileToBucketLog>();
        options.AddActivity<CopyFileInBucketActivity, CopyFileInBucketArguments, CopyFileInBucketLog>();
        options.AddActivity<ConsumeUploadIntentsActivity, ConsumeUploadIntentsArguments, ConsumeUploadIntentsLog>();
    }
}
