using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Application.Shared;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Pictures;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class YouTubeTrackRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
    {
        public RoutingSlipBuilder BuildAudioWorkflow(DownloadYouTubeTrackEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddActivity(
                ActivityNames.GenerateAudioWorkflowPaths,
                EndpointHelper.BuildExecuteActivityUri(GenerateAudioWorkflowPathsActivity.ExecuteEndpointName),
                new GenerateAudioWorkflowPathsArguments(
                    message.TrackId,
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    $"{message.VideoId}.m4a"));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadYouTubeAudio,
                EndpointHelper.BuildExecuteActivityUri(DownloadYouTubeAudioActivity.ExecuteEndpointName),
                new DownloadYouTubeAudioArguments(
                    message.TrackId,
                    message.VideoId,
                    RoutingSlipVariableNames.Audio.SourceFilePath));

            routingSlipBuilder.AddActivity(
                ActivityNames.TranscodeAudio,
                EndpointHelper.BuildExecuteActivityUri(TranscodeAudioActivity.ExecuteEndpointName),
                new TranscodeAudioArguments(
                    message.TrackId,
                    RoutingSlipVariableNames.Audio.SourceFilePath,
                    RoutingSlipVariableNames.Workflow.TemporalDirectory));

            routingSlipBuilder.AddActivity(
                ActivityNames.TransferFiles,
                EndpointHelper.BuildExecuteActivityUri(TransferFilesToBucketActivity.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    message.Bucket,
                    RoutingSlipVariableNames.Audio.TranscodedDirectory,
                    message.AudioProcessedFolderKey));

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdateTrackAudio,
                EndpointHelper.BuildExecuteActivityUri(UpdateTrackAudioActivity.ExecuteEndpointName),
                new UpdateTrackAudioArguments(
                    message.TrackId,
                    message.AudioProcessedFolderKey,
                    RoutingSlipVariableNames.Audio.DurationSeconds));

            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.SubjectId, message.TrackId);
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.ProcessKind, RoutingSlipVariableNames.ProcessKinds.TrackAudio);
            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(ProcessingSlipFaultConsumer.QueueName),
                RoutingSlipEvents.Faulted);

            return routingSlipBuilder;
        }

        public RoutingSlipBuilder BuildPictureWorkflow(DownloadYouTubeTrackEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddActivity(
                ActivityNames.GeneratePictureWorkflowPaths,
                EndpointHelper.BuildExecuteActivityUri(GeneratePictureWorkflowPathsActivity.ExecuteEndpointName),
                new GeneratePictureWorkflowPathsArguments(
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    $"{message.VideoId}.jpg"));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadThumbnail,
                EndpointHelper.BuildExecuteActivityUri(DownloadFileFromUrlActivity.ExecuteEndpointName),
                new DownloadFileFromUrlArguments(
                    message.ThumbnailUrl,
                    RoutingSlipVariableNames.Picture.OriginalFilePath));

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeSmall,
                ActivityNames.UploadSmall,
                message.Bucket,
                RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                message.Small);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeMedium,
                ActivityNames.UploadMedium,
                message.Bucket,
                RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                message.Medium);

            AddResizeActivity(
                routingSlipBuilder,
                ActivityNames.ResizeLarge,
                ActivityNames.UploadLarge,
                message.Bucket,
                RoutingSlipVariableNames.Picture.LargeResizedFilePath,
                message.Large);

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdateTrackPicture,
                EndpointHelper.BuildExecuteActivityUri(UpdateTrackPictureActivity.ExecuteEndpointName),
                new UpdateTrackPictureArguments(
                    message.TrackId,
                    string.Empty,
                    RoutingSlipVariableNames.Picture.SmallResizedFilePath,
                    RoutingSlipVariableNames.Picture.MediumResizedFilePath,
                    RoutingSlipVariableNames.Picture.LargeResizedFilePath));

            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.SubjectId, message.TrackId);
            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.ProcessKind, RoutingSlipVariableNames.ProcessKinds.TrackPicture);
            routingSlipBuilder.AddSubscription(
                EndpointHelper.BuildConsumerUri(ProcessingSlipFaultConsumer.QueueName),
                RoutingSlipEvents.Faulted);

            return routingSlipBuilder;
        }

        static void AddResizeActivity(
            RoutingSlipBuilder routingSlipBuilder,
            string resizeActivityName,
            string uploadActivityName,
            string bucketName,
            string destinationFilePathVariableName,
            ImageSize imageSize)
        {
            routingSlipBuilder.AddActivity(
                resizeActivityName,
                EndpointHelper.BuildExecuteActivityUri(ResizePictureActivity.ExecuteEndpointName),
                new ResizePictureLocalArguments(
                    RoutingSlipVariableNames.Picture.OriginalFilePath,
                    destinationFilePathVariableName,
                    imageSize.Width,
                    imageSize.Height));

            routingSlipBuilder.AddActivity(
                uploadActivityName,
                EndpointHelper.BuildExecuteActivityUri(UploadFileToBucketActivity.ExecuteEndpointName),
                new UploadFileToBucketArguments(
                    destinationFilePathVariableName,
                    bucketName,
                    imageSize.SavePath));
        }
    }
}
