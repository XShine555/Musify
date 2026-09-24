using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.LifeCycle;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Consumers
{
    public class AlbumProcessingFailedConsumer(IBus bus) : IConsumer<AlbumProcessingFailed>
    {
        public Task Consume(ConsumeContext<AlbumProcessingFailed> context)
        {
            var message = context.Message;

            return ProcessingFailedCleanup.ExecuteAsync(
                bus, message.AlbumId, ActivityNames.MarkAlbumAsFailed, MarkAlbumLifeCycleActivity.ExecuteEndpointName, message.Bucket,
                (ActivityNames.RemoveAlbumOriginalPicture, message.PictureKey));
        }
    }
}
