using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Activities.Files;
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
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            routingSlipBuilder.AddActivity(
                ActivityNames.GenerateAudioWorkflowPaths,
                EndpointHelper.BuildExecuteActivityUri(GenerateAudioWorkflowPathsActivity.ExecuteEndpointName),
                new GenerateAudioWorkflowPathsArguments(
                    message.TrackId,
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    message.SourceKey));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadFile,
                EndpointHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    message.SourceBucket,
                    message.SourceKey,
                    RoutingSlipVariableNames.Audio.SourceFilePath));

            routingSlipBuilder.AddActivity(
                ActivityNames.TranscodeAudio,
                EndpointHelper.BuildExecuteActivityUri(TranscodeDashAudioActivity.ExecuteEndpointName),
                new TranscodeDashAudioArguments(
                    message.TrackId,
                    RoutingSlipVariableNames.Audio.SourceFilePath,
                    RoutingSlipVariableNames.Workflow.TemporalDirectory));

            routingSlipBuilder.AddActivity(
                ActivityNames.TransferFiles,
                EndpointHelper.BuildExecuteActivityUri(TransferFilesToBucketActivity.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    message.DestinationBucket,
                    RoutingSlipVariableNames.Audio.TranscodedDirectory,
                    message.DestinationFolderKey));

            routingSlipBuilder.AddActivity(
                ActivityNames.UpdateTrackAudio,
                EndpointHelper.BuildExecuteActivityUri(UpdateTrackAudioActivity.ExecuteEndpointName),
                new UpdateTrackAudioArguments(
                    message.TrackId,
                   RoutingSlipVariableNames.Audio.TranscodedDirectory));

            return routingSlipBuilder;
        }
    }
}
