using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.RoutingSlip.Builders
{
    public class AudioWorkflowRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
    {
        public RoutingSlipBuilder Build(TranscodeAudioFromTrackEvent message, Guid? correlationId)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            routingSlipBuilder.AddVariable(RoutingSlipVariableNames.Workflow.CorrelationId, correlationId ?? Guid.Empty);

            routingSlipBuilder.AddActivity(
                ActivityNames.GenerateAudioWorkflowPaths,
                Consumers.MessagingHelper.BuildExecuteActivityUri(GenerateAudioWorkflowPathsActivity.ExecuteEndpointName),
                new GenerateAudioWorkflowPathsArguments(
                    workerConfiguration.Routes.TemporaryFilesDirectory,
                    message.SourceKeyName));

            routingSlipBuilder.AddActivity(
                ActivityNames.DownloadFile,
                Consumers.MessagingHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    message.SourceBucketName,
                    message.SourceKeyName,
                    RoutingSlipVariableNames.Audio.SourceFilePath));

            routingSlipBuilder.AddActivity(
                ActivityNames.TranscodeAudio,
                Consumers.MessagingHelper.BuildExecuteActivityUri(TranscodeDashAudioActivity.ExecuteEndpointName),
                new TranscodeDashAudioArguments(
                    RoutingSlipVariableNames.Audio.SourceFilePath,
                    RoutingSlipVariableNames.Workflow.TempDirectory));

            routingSlipBuilder.AddActivity(
                ActivityNames.UploadFiles,
                Consumers.MessagingHelper.BuildExecuteActivityUri(TransferFilesToBucket.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    message.DestinationBucketName,
                    RoutingSlipVariableNames.Audio.TranscodedDirectory,
                    message.DestinationKeyName));

            return routingSlipBuilder;
        }
    }
}
