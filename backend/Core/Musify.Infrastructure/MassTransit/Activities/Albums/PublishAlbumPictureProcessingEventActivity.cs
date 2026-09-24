using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.Albums
{
    internal class PublishAlbumPictureProcessingEventActivity(
        IPublishEndpoint publishEndpoint,
        ILogger<PublishAlbumPictureProcessingEventActivity> logger)
        : IExecuteActivity<PublishAlbumPictureProcessingEventArguments>
    {
        public const string ExecuteEndpointName = "publish-album-picture-processing-event";

        public async Task<ExecutionResult> Execute(ExecuteContext<PublishAlbumPictureProcessingEventArguments> executeContext)
        {
            var arguments = executeContext.Arguments;

            await publishEndpoint.Publish(
                new UpdateAlbumPictureEvent(
                    arguments.AlbumId,
                    arguments.Bucket,
                    arguments.FinalPictureKey,
                    arguments.Sizes),
                executeContext.CancellationToken);

            logger.LogInformation("Published picture processing event for album {AlbumId}", arguments.AlbumId);

            return executeContext.Completed();
        }
    }
}
