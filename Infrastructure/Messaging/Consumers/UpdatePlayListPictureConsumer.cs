using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class UpdatePlayListPictureConsumer(IBus bus)
        : IConsumer<UpdatePlayListPictureEvent>
    {
        public const string QueueName = "Update-PlayList-Picture";

        public async Task Consume(ConsumeContext<UpdatePlayListPictureEvent> consumeContext)
        {
            var routingSlipBuilder = new RoutingSlipBuilder(NewId.NextGuid());

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeSmall",
                consumeContext.Message.SmallPictureKeyName,
                consumeContext.Message.SmallPictureWidth,
                consumeContext.Message.SmallPictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeMedium",
                consumeContext.Message.MediumPictureKeyName,
                consumeContext.Message.MediumPictureWidth,
                consumeContext.Message.MediumPictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeLarge",
                consumeContext.Message.LargePictureKeyName,
                consumeContext.Message.LargePictureWidth,
                consumeContext.Message.LargePictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName);

            routingSlipBuilder.AddActivity(
                "DeleteOriginal",
                BuildExecuteUri(RemoveFilesActivity.ExecuteEndpointName),
                new RemoveFileArguments(consumeContext.Message.BucketName, consumeContext.Message.OriginalPictureKeyName));

            routingSlipBuilder.AddActivity(
                "UpdatePlayListPicture",
                BuildExecuteUri(UpdatePlayListPictureActivity.ExecuteEndpointName),
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
            string bucketName)
        {
            routingSlipBuilder.AddActivity(
                activityName,
                BuildExecuteUri(ResizePictureActivity.ExecuteEndpointName),
                new ResizePictureArgument(
                    bucketName,
                    originalPictureKeyName,
                    bucketName,
                    destinationKeyName,
                    width,
                    height));
        }

        static Uri BuildExecuteUri(string endpointName) => new($"queue:{endpointName}_execute");
    }
}