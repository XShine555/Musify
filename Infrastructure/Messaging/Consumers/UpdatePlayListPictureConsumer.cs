using MassTransit;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class UpdatePlayListPictureConsumer(IBus bus, IPictureHandler pictureHandler,
        StorageSettings storageSettings, PlayListConfiguration playListConfiguration)
        : IConsumer<UpdatePlayListPictureEvent>
    {
        public const string QueueName = "Update-PlayList-Picture";

        public async Task Consume(ConsumeContext<UpdatePlayListPictureEvent> consumeContext)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            var smallPictureKeyName = Path.Combine(
                playListConfiguration.Routes.SmallPictures,
                $"{consumeContext.Message.PlayListId}_small.{pictureHandler.FileExtension}");
            var mediumPictureKeyName = Path.Combine(
                playListConfiguration.Routes.MediumPictures,
                $"{consumeContext.Message.PlayListId}_medium.{pictureHandler.FileExtension}");
            var largePictureKeyName = Path.Combine(
                playListConfiguration.Routes.LargePictures,
                $"{consumeContext.Message.PlayListId}_large.{pictureHandler.FileExtension}");

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeSmall",
                smallPictureKeyName,
                playListConfiguration.PicturesSizes.SmallPictureWidth,
                playListConfiguration.PicturesSizes.SmallPictureHeight,
                consumeContext.Message.OriginalPictureKeyName);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeMedium",
                mediumPictureKeyName,
                playListConfiguration.PicturesSizes.MediumPictureWidth,
                playListConfiguration.PicturesSizes.MediumPictureHeight,
                consumeContext.Message.OriginalPictureKeyName);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeLarge",
                largePictureKeyName,
                playListConfiguration.PicturesSizes.LargePictureWidth,
                playListConfiguration.PicturesSizes.LargePictureHeight,
                consumeContext.Message.OriginalPictureKeyName);

            routingSlipBuilder.AddActivity(
                "DeleteOriginal",
                BuildExecuteUri(RemoveFilesActivity.ExecuteEndpointName),
                new RemoveFileArguments(storageSettings.BucketName, consumeContext.Message.OriginalPictureKeyName));

            routingSlipBuilder.AddActivity(
                "UpdatePlayListPicture",
                BuildExecuteUri(UpdatePlayListPictureActivity.ExecuteEndpointName),
                new UpdatePlayListPictureArguments(
                    consumeContext.Message.PlayListId,
                    smallPictureKeyName,
                    mediumPictureKeyName,
                    largePictureKeyName));

            var routingSlip = routingSlipBuilder.Build();
            await bus.Execute(routingSlip);
        }

        void AddResizeActivity(
            RoutingSlipBuilder routingSlipBuilder,
            string activityName,
            string destinationKeyName,
            int width,
            int height,
            string originalPictureKeyName)
        {
            routingSlipBuilder.AddActivity(
                activityName,
                BuildExecuteUri(ResizePictureActivity.ExecuteEndpointName),
                new ResizePictureArgument(
                    storageSettings.BucketName,
                    originalPictureKeyName,
                    storageSettings.BucketName,
                    destinationKeyName,
                    width,
                    height));
        }

        static Uri BuildExecuteUri(string endpointName) => new($"queue:{endpointName}_execute");
    }
}