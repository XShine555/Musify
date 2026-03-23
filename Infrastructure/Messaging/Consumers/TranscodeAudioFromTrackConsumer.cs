using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class TranscodeAudioFromTrackConsumer(
        IBus bus,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IConsumer<TranscodeAudioFromTrackEvent>
    {
        public const string QueueName = "Transcode-Audio-From-Track";

        public async Task Consume(ConsumeContext<TranscodeAudioFromTrackEvent> consumeContext)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            var fileName = Path.GetFileName(consumeContext.Message.SourceKeyName);
            var folderName = Guid.NewGuid().ToString();
            var workingDirectory = Path.Combine(audioTranscoderConfiguration.Routes.WorkingDirectory, folderName);

            routingSlipBuilder.AddActivity(
                "DownloadFile",
                MessagingHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    consumeContext.Message.SourceBucketName,
                    consumeContext.Message.SourceKeyName,
                    workingDirectory));

            routingSlipBuilder.AddActivity(
                "TranscodeAudio",
                MessagingHelper.BuildExecuteActivityUri(TranscodeDashAudioActivity.ExecuteEndpointName),
                new TranscodeDashAudioArguments(
                    Path.Combine(workingDirectory, fileName),
                    folderName));

            routingSlipBuilder.AddActivity(
                "UploadFile",
                MessagingHelper.BuildExecuteActivityUri(TransferFilesToBucket.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    workingDirectory,
                    consumeContext.Message.DestinationBucketName,
                    consumeContext.Message.DestinationKeyName));

            var routingSlip = routingSlipBuilder.Build();
            await bus.Execute(routingSlip);
        }
    }
}