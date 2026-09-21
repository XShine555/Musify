using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.Albums;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class AlbumProcessingFailedConsumer(IBus bus) : IConsumer<AlbumProcessingFailed>
    {
        public async Task Consume(ConsumeContext<AlbumProcessingFailed> context)
        {
            var message = context.Message;

            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddSubscription(
                EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
                RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            if (!string.IsNullOrWhiteSpace(message.Bucket) && !string.IsNullOrWhiteSpace(message.PictureKey))
                builder.AddActivity(
                    ActivityNames.RemoveAlbumOriginalPicture,
                    EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                    new RemoveFileFromBucketArguments(message.Bucket, message.PictureKey));

            builder.AddActivity(
                ActivityNames.MarkAlbumAsFailed,
                EndpointHelper.BuildExecuteActivityUri(MarkAlbumAsFailedActivity.ExecuteEndpointName),
                new MarkAlbumAsFailedArguments(message.AlbumId));

            await bus.Execute(builder.Build());
        }
    }
}
