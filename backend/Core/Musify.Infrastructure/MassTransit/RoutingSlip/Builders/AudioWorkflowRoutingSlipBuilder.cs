using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Activities.Audio;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.RoutingSlip.Builders;

public class AudioWorkflowRoutingSlipBuilder(WorkerConfiguration workerConfiguration)
{
    public RoutingSlipBuilder Build(UpdateTrackAudioEvent message, Guid? correlationId) =>
        RoutingSlips.Create(correlationId)
            .AddStep(
                ActivityNames.GenerateAudioWorkflowPaths,
                GenerateAudioWorkflowPathsActivity.ExecuteEndpointName,
                new GenerateAudioWorkflowPathsArguments(
                    message.TrackId, workerConfiguration.Routes.TemporaryFilesDirectory, message.SourceKey))
            .AddStep(
                ActivityNames.DownloadFile,
                DownloadFileFromBucketActivity.ExecuteEndpointName,
                new DownloadFileFromBucketArguments(
                    message.SourceBucket, message.SourceKey, RoutingSlipVariableNames.Audio.SourceFilePath))
            .AddStep(
                ActivityNames.TranscodeAudio,
                TranscodeAudioActivity.ExecuteEndpointName,
                new TranscodeAudioArguments(
                    message.TrackId,
                    RoutingSlipVariableNames.Audio.SourceFilePath,
                    RoutingSlipVariableNames.Workflow.TemporalDirectory))
            .AddStep(
                ActivityNames.TransferFiles,
                TransferFilesToBucketActivity.ExecuteEndpointName,
                new TransferFilesToBucketArguments(
                    message.DestinationBucket,
                    RoutingSlipVariableNames.Audio.TranscodedDirectory,
                    message.DestinationFolderKey))
            .AddStep(
                ActivityNames.UpdateTrackAudio,
                UpdateTrackAudioActivity.ExecuteEndpointName,
                new UpdateTrackAudioArguments(
                    message.TrackId, message.DestinationFolderKey, RoutingSlipVariableNames.Audio.DurationSeconds))
            .TrackFaults(message.TrackId, RoutingSlipVariableNames.ProcessKinds.TrackAudio);
}
