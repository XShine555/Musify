using MassTransit;
using Musify.Application.Configuration;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
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

            routingSlipBuilder.AddActivity(
                "ResizeSmall",
                new Uri("Queue:Resize-Picture_Execute"),
                new ResizePictureArgument(
                    storageSettings.BucketName,
                    consumeContext.Message.OriginalPictureKeyName,
                    storageSettings.BucketName,
                    playListConfiguration.Routes.SmallPictures,
                    playListConfiguration.PicturesSizes.SmallPictureWidth,
                    playListConfiguration.PicturesSizes.SmallPictureHeight));
            
            routingSlipBuilder.AddActivity(
                "ResizeMedium",
                new Uri("Queue:Resize-Picture_Execute"),
                new ResizePictureArgument(
                    storageSettings.BucketName,
                    consumeContext.Message.OriginalPictureKeyName,
                    storageSettings.BucketName,
                    playListConfiguration.Routes.MediumPictures,
                    playListConfiguration.PicturesSizes.MediumPictureWidth,
                    playListConfiguration.PicturesSizes.MediumPictureHeight));

            routingSlipBuilder.AddActivity(
                "ResizeLarge",
                new Uri("Queue:Resize-Picture_Execute"),
                new ResizePictureArgument(
                    storageSettings.BucketName,
                    consumeContext.Message.OriginalPictureKeyName,
                    storageSettings.BucketName,
                    playListConfiguration.Routes.LargePictures,
                    playListConfiguration.PicturesSizes.LargePictureWidth,
                    playListConfiguration.PicturesSizes.LargePictureHeight));

            routingSlipBuilder.AddActivity(
                "DeleteOriginal",
                new Uri("Queue:Delete-Picture_Execute"),
                new RemoveFileArguments(
                    storageSettings.BucketName,
                    consumeContext.Message.OriginalPictureKeyName));

            routingSlipBuilder.AddActivity(
                "UpdatePlayListPicture",
                new Uri("Queue:Update-PlayList-Picture_Execute"),
                new UpdatePlayListPictureArguments(
                    consumeContext.Message.PlayListId,
                    smallPictureKeyName,
                    mediumPictureKeyName,
                    largePictureKeyName));

            var routingSlip = routingSlipBuilder.Build();
            await bus.Execute(routingSlip);
        }
    }
}