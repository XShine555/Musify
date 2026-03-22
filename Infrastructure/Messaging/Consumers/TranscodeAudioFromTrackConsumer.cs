using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class TranscodeAudioFromTrackConsumer(IBus bus, AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IConsumer<TranscodeAudioFromTrackEvent>
    {
        public const string QueueName = "Transcode-Audio-From-Track";

        public async Task Consume(ConsumeContext<TranscodeAudioFromTrackEvent> consumeContext)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            var folderName = Guid.NewGuid().ToString();
            var workingDirectory = Path.Combine("D:\\tempsFilesDev\\");

            routingSlipBuilder.AddActivity(
                "DownloadFile",
                BuildExecuteUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    consumeContext.Message.SourceBucketName,
                    consumeContext.Message.SourceKeyName,
                    workingDirectory)
                );
            var fileName = Path.GetFileName(consumeContext.Message.SourceKeyName);

            routingSlipBuilder.AddActivity(
                "TranscodeAudio",
                BuildExecuteUri(TranscodeDashAudioActivity.ExecuteEndpointName),
                new TranscodeDashAudioArguments(
                    $"D:\\tempsFilesDev\\{fileName}",
                    "D:\\tempsFilesDev\\")
                );

            routingSlipBuilder.AddActivity(
                "UploadFile",
                BuildExecuteUri(TransferFilesToBucket.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    workingDirectory,
                    consumeContext.Message.DestinationBucketName,
                    consumeContext.Message.DestinationKeyName)
                );

            var routingSlip = routingSlipBuilder.Build();
            await bus.Execute(routingSlip);
        }

        static Uri BuildExecuteUri(string endpointName) => new($"queue:{endpointName}_execute");
    }
}