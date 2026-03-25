using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.MassTransit.Activities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders
{
    public class AudioWorkflowRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
    {
        public RoutingSlipBuilder Build(UpdateTrackAudioEvent message, Guid? correlationId) 
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddSubscription(
                MessagingHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddActivity(
                ActivityNames.GenerateAudioWorkflowPaths,
                MessagingHelper.BuildExecuteActivityUri(GenerateAudioWorkflowPathsActivity.ExecuteEndpointName),
                new GenerateAudioWorkflowPathsArguments(
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    message.SourceKeyName));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadFile,
                MessagingHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    message.SourceBucketName,
                    message.SourceKeyName,
                    RoutingSlipVariableNames.Audio.SourceFilePath));

            routingSlipBuilder.AddActivity(
                ActivityNames.TranscodeAudio,
                MessagingHelper.BuildExecuteActivityUri(TranscodeDashAudioActivity.ExecuteEndpointName),
                new TranscodeDashAudioArguments(
                    RoutingSlipVariableNames.Audio.SourceFilePath,
                    RoutingSlipVariableNames.Workflow.TemporalDirectory));

            routingSlipBuilder.AddActivity(
                ActivityNames.UploadFiles,
                MessagingHelper.BuildExecuteActivityUri(TransferFilesToBucketActivity.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    message.DestinationBucketName,
                    RoutingSlipVariableNames.Audio.TranscodedDirectory,
                    message.DestinationFolderKeyName));

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdateTrackAudio,
                MessagingHelper.BuildExecuteActivityUri(UpdateTrackAudioActivity.ExecuteEndpointName),
                new UpdateTrackAudioArguments(
                    message.TrackId,
                    message.DestinationFolderKeyName));

            return routingSlipBuilder;
        }
    }
}
