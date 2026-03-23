using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class UpdatePlayListPictureConsumer(
        IBus bus,
        AudioTranscoderConfiguration audioTranscoderConfiguration)
        : IConsumer<UpdatePlayListPictureEvent>
    {
        public const string QueueName = "Update-PlayList-Picture";

        public async Task Consume(ConsumeContext<UpdatePlayListPictureEvent> consumeContext)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());
            var folderName = Guid.NewGuid().ToString();
            var workingDirectory = Path.Combine(audioTranscoderConfiguration.Routes.WorkingDirectory, folderName);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeSmall",
                consumeContext.Message.SmallPictureKeyName,
                consumeContext.Message.SmallPictureWidth,
                consumeContext.Message.SmallPictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName,
                workingDirectory);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeMedium",
                consumeContext.Message.MediumPictureKeyName,
                consumeContext.Message.MediumPictureWidth,
                consumeContext.Message.MediumPictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName,
                workingDirectory);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeLarge",
                consumeContext.Message.LargePictureKeyName,
                consumeContext.Message.LargePictureWidth,
                consumeContext.Message.LargePictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName,
                workingDirectory);

            routingSlipBuilder.AddActivity(
                "DeleteOriginal",
                MessagingHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new RemoveFileFromBucketArguments(
                    consumeContext.Message.BucketName,
                    consumeContext.Message.OriginalPictureKeyName));

            routingSlipBuilder.AddActivity(
                "UploadFiles",
                MessagingHelper.BuildExecuteActivityUri(TransferFilesToBucket.ExecuteEndpointName),
                new TransferFilesToBucketArguments(
                    workingDirectory,
                    consumeContext.Message.BucketName,
                    folderName));

            routingSlipBuilder.AddActivity(
                "UpdatePlayListPicture",
                MessagingHelper.BuildExecuteActivityUri(UpdatePlayListPictureActivity.ExecuteEndpointName),
                new UpdatePlayListPictureArguments(
                    consumeContext.Message.PlayListId,
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