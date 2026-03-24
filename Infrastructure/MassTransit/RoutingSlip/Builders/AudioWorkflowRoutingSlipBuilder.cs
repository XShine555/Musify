using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Consumers;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;
using Musify.Infrastructure.Messaging.Consumers;

namespace Musify.Infrastructure.Messaging.RoutingSlip.Builders
{
    public class AudioWorkflowRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
    {
        public RoutingSlipBuilder Build(TranscodeAudioFromTrackEvent message, Guid? correlationId)
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
                    message.DestinationKeyName));

            return routingSlipBuilder;
        }
    }
}
