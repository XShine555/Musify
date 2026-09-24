using MassTransit;
using MassTransit.Courier.Contracts;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.Files;
using Musify.Infrastructure.MassTransit.Activities.PlayLists;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class PlayListProcessingFailedConsumer(IBus bus) : IConsumer<PlayListProcessingFailed>
{
    public async Task Consume(ConsumeContext<PlayListProcessingFailed> context)
    {
        var message = context.Message;

        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        builder.AddSubscription(
            EndpointHelper.BuildConsumerUri(RoutingSlipCleanUpConsumer.QueueName),
            RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        if (!string.IsNullOrWhiteSpace(message.Bucket) && !string.IsNullOrWhiteSpace(message.PictureKey))
        {
            builder.AddActivity(
                ActivityNames.RemovePlayListOriginalPicture,
                EndpointHelper.BuildExecuteActivityUri(RemoveFileFromBucketActivity.ExecuteEndpointName),
                new RemoveFileFromBucketArguments(message.Bucket, message.PictureKey));
        }

        builder.AddActivity(
            ActivityNames.MarkPlayListAsFailed,
            EndpointHelper.BuildExecuteActivityUri(MarkPlayListAsFailedActivity.ExecuteEndpointName),
            new MarkPlayListAsFailedArguments(message.PlayListId));

        await bus.Execute(builder.Build());
    }
}
