using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.Messaging.Activities;
using Musify.Infrastructure.Messaging.Activities.Arguments;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class UpdateTrackPictureConsumer(IBus bus)
        : IConsumer<UpdateTrackPictureEvent>
    {
        public const string QueueName = "update-track-picture";

        public async Task Consume(ConsumeContext<UpdateTrackPictureEvent> consumeContext)
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
                consumeContext.Message.SmallPictureKeyName,
                consumeContext.Message.MediumPictureWidth,
                consumeContext.Message.MediumPictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName);

            AddResizeActivity(
                routingSlipBuilder,
                "ResizeMedium",
                consumeContext.Message.SmallPictureKeyName,
                consumeContext.Message.LargePictureWidth,
                consumeContext.Message.LargePictureHeight,
                consumeContext.Message.OriginalPictureKeyName,
                consumeContext.Message.BucketName);

            routingSlipBuilder.AddActivity(
                "DeleteOriginal",
                BuildExecuteUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new RemoveFileFromBucketArguments(
                    consumeContext.Message.BucketName,
                    consumeContext.Message.OriginalPictureKeyName)
                );

            routingSlipBuilder.AddActivity(
                "UpdateTrackPictureActivity",
                BuildExecuteUri(UpdateTrackPictureActivity.ExecuteEndpointName),
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