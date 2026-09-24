using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.Tracks;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class TrackProcessingFailedConsumer(IBus bus) : IConsumer<TrackProcessingFailed>
{
    public async Task Consume(ConsumeContext<TrackProcessingFailed> context)
    {
        var message = context.Message;

        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
            RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        if (!string.IsNullOrWhiteSpace(message.Bucket) && !string.IsNullOrWhiteSpace(message.PictureKey))
        {
            builder.AddActivity(
                ActivityNames.RemoveTrackOriginalPicture,
                EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new RemoveFileFromBucketArguments(message.Bucket, message.PictureKey));
        }

        if (!string.IsNullOrWhiteSpace(message.Bucket) && !string.IsNullOrWhiteSpace(message.AudioKey))
        {
            builder.AddActivity(
                ActivityNames.RemoveTrackOriginalAudio,
                EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new RemoveFileFromBucketArguments(message.Bucket, message.AudioKey));
        }

        builder.AddActivity(
            ActivityNames.MarkTrackAsFailed,
            EndpointHelper.BuildExecuteActivityUri(MarkTrackAsFailedActivity.ExecuteEndpointName),
            new MarkTrackAsFailedArguments(message.TrackId));

        await bus.Execute(builder.Build());
    }
}
