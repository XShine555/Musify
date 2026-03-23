using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class UpdateTrackPictureConsumer(
        IBus bus,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IConsumer<UpdateTrackPictureEvent>
    {
        public const string QueueName = "Update-Track-Picture";

        public async Task Consume(ConsumeContext<UpdateTrackPictureEvent> consumeContext)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            var folderName = Guid.NewGuid().ToString();
            var workingDirectory = Path.Combine(audioTranscoderConfiguration.Routes.WorkingDirectory, folderName);

            routingSlipBuilder.AddActivity(
                "DownloadFile",
                MessagingHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    consumeContext.Message.SourceBucketName,
                    consumeContext.Message.SourceKeyName,
                    workingDirectory));

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeSmall",
                consumeContext.Message.SmallPictureKeyName,
                consumeContext.Message.SmallPictureWidth,
                consumeContext.Message.SmallPictureHeight,
                consumeContext.Message.SourceKeyName,
                consumeContext.Message.SourceBucketName,
                workingDirectory);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeMedium",
                consumeContext.Message.MediumPictureKeyName,
                consumeContext.Message.MediumPictureWidth,
                consumeContext.Message.MediumPictureHeight,
                consumeContext.Message.SourceKeyName,
                consumeContext.Message.SourceBucketName,
                workingDirectory);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeLarge",
                consumeContext.Message.LargePictureKeyName,
                consumeContext.Message.LargePictureWidth,
                consumeContext.Message.LargePictureHeight,
                consumeContext.Message.SourceKeyName,
                consumeContext.Message.SourceBucketName,
                workingDirectory);

            routingSlipBuilder.AddActivity(
                "UploadFiles",
                MessagingHelper.BuildExecuteActivityUri(TransferFilesToBucket.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    workingDirectory,
                    consumeContext.Message.SourceBucketName,
                    folderName));

            routingSlipBuilder.AddActivity(
                "UpdateTrackPicture",
                MessagingHelper.BuildExecuteActivityUri(UpdateTrackPictureActivity.ExecuteEndpointName),
                new UpdateTrackPictureArguments(
                    consumeContext.Message.TrackId,
                    consumeContext.Message.SmallPictureKeyName,
                    consumeContext.Message.MediumPictureKeyName,
                    consumeContext.Message.LargePictureKeyName));

            var routingSlip = routingSlipBuilder.Build();
            await bus.Execute(routingSlip);
        }

        void AddResizeActivity(
            RoutingSlipBuilder routingSlipBuilder,
            string activityName,
            string destinationKeyName,
            int width,
            int height,
            string originalPictureKeyName,
            string bucketName,
            string workingDirectory)
        {
            var fileName = Path.GetFileName(originalPictureKeyName);
            var sourceFilePath = Path.Combine(workingDirectory, fileName);
            var destinationFileName = Path.GetFileNameWithoutExtension(destinationKeyName) + Path.GetExtension(fileName);
            var destinationFilePath = Path.Combine(workingDirectory, destinationFileName);

            // First: Download original file
            routingSlipBuilder.AddActivity(
                $"{activityName}_Download",
                MessagingHelper.BuildExecuteActivityUri(DownloadFileFromBucketActivity.ExecuteEndpointName),
                new DownloadFileFromBucketArguments(
                    bucketName,
                    originalPictureKeyName,
                    workingDirectory));

            // Second: Resize picture
            routingSlipBuilder.AddActivity(
                activityName,
                MessagingHelper.BuildExecuteActivityUri(ResizePictureActivity.ExecuteEndpointName),
                new ResizePictureLocalArguments(
                    sourceFilePath,
                    destinationFilePath,
                    width,
                    height));
        }
    }
}