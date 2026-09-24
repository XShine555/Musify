using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Tracks;

internal class PublishTrackProcessingEventsActivity(
    IPublishEndpoint publishEndpoint,
    ILogger<PublishTrackProcessingEventsActivity> logger)
    : IExecuteActivity<PublishTrackProcessingEventsArguments>
{
    public const string ExecuteEndpointName = "publish-track-processing-events";

    public async Task<ExecutionResult> Execute(ExecuteContext<PublishTrackProcessingEventsArguments> executeContext)
    {
        var arguments = executeContext.Arguments;

        await publishEndpoint.Publish(
            new UpdateTrackPictureEvent(
                arguments.TrackId,
                arguments.Bucket,
                arguments.FinalPictureKey,
                arguments.Sizes),
            executeContext.CancellationToken);

        await publishEndpoint.Publish(
            new UpdateTrackAudioEvent(
                arguments.TrackId,
                arguments.Bucket,
                arguments.FinalAudioKey,
                arguments.Bucket,
                arguments.AudioDestinationFolderKey),
            executeContext.CancellationToken);

        logger.LogInformation("Published processing events for track {TrackId}", arguments.TrackId);

        return executeContext.Completed();
    }
}
