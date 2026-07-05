using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Events;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.PlayLists
{
    internal class PublishPlayListPictureProcessingEventActivity(
        IPublishEndpoint publishEndpoint,
        ILogger<PublishPlayListPictureProcessingEventActivity> logger)
        : IExecuteActivity<PublishPlayListPictureProcessingEventArguments>
    {
        public const string ExecuteEndpointName = "publish-play-list-picture-processing-event";

        public async Task<ExecutionResult> Execute(ExecuteContext<PublishPlayListPictureProcessingEventArguments> executeContext)
        {
            var arguments = executeContext.Arguments;

            await publishEndpoint.Publish(
                new UpdatePlayListPictureEvent(
                    arguments.PlayListId,
                    arguments.Bucket,
                    arguments.FinalPictureKey,
                    arguments.Small,
                    arguments.Medium,
                    arguments.Large),
                executeContext.CancellationToken);

            logger.LogInformation("Published picture processing event for playlist {PlayListId}", arguments.PlayListId);

            return executeContext.Completed();
        }
    }
}
