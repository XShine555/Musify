using MassTransit;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Activities.LifeCycle;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Consumers;

public class TrackProcessingFailedConsumer(IBus bus) : IConsumer<TrackProcessingFailed>
{
    public Task Consume(ConsumeContext<TrackProcessingFailed> context)
    {
        var message = context.Message;

        return ProcessingFailedCleanup.ExecuteAsync(
            bus, message.TrackId, ActivityNames.MarkTrackAsFailed, MarkTrackLifeCycleActivity.ExecuteEndpointName, message.Bucket,
            (ActivityNames.RemoveTrackOriginalPicture, message.PictureKey),
            (ActivityNames.RemoveTrackOriginalAudio, message.AudioKey));
    }
}
