using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.LifeCycle;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class PlayListProcessingFailedConsumer(IBus bus) : IConsumer<PlayListProcessingFailed>
    {
        public Task Consume(ConsumeContext<PlayListProcessingFailed> context)
        {
            var message = context.Message;

            return ProcessingFailedCleanup.ExecuteAsync(
                bus, message.PlayListId, ActivityNames.MarkPlayListAsFailed, MarkPlayListLifeCycleActivity.ExecuteEndpointName, message.Bucket,
                (ActivityNames.RemovePlayListOriginalPicture, message.PictureKey));
        }
    }
}
