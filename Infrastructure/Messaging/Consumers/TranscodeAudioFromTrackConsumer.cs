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

            routingSlipBuilder.AddActivity(
                "DownloadFile",
                MessagingHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    consumeContext.Message.SourceBucketName,
                    consumeContext.Message.SourceKeyName,
                    audioTranscoderConfiguration.Routes.WorkingDirectory));

            routingSlipBuilder.AddActivity(
                "TranscodeAudio",
                MessagingHelper.BuildExecuteActivityUri(TranscodeDashAudioActivity.ExecuteEndpointName),
                new TranscodeDashAudioArguments());

            routingSlipBuilder.AddActivity(
                "UploadFile",
                MessagingHelper.BuildExecuteActivityUri(TransferFilesToBucket.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    consumeContext.Message.DestinationBucketName,
                    consumeContext.Message.DestinationKeyName));

            var routingSlip = routingSlipBuilder.Build();
            await bus.Execute(routingSlip);
        }
    }
}