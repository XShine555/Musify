using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.PlayLists
{
    internal class MarkPlayListAsFailedActivity(
        IDatabase database,
        ILogger<MarkPlayListAsFailedActivity> logger)
        : IExecuteActivity<MarkPlayListAsFailedArguments>
    {
        public const string ExecuteEndpointName = "mark-play-list-as-failed";

        public async Task<ExecutionResult> Execute(ExecuteContext<MarkPlayListAsFailedArguments> executeContext)
        {
            var playList = await database.PlayLists
                .SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.PlayListId, executeContext.CancellationToken);

            if (playList != null)
            {
                playList.LifeCycleStatus = LifeCycleStatus.Failed;
                await database.SaveChangesAsync(executeContext.CancellationToken);
                logger.LogInformation("Marked playlist {PlayListId} as failed", executeContext.Arguments.PlayListId);
            }

            return executeContext.Completed();
        }
    }
}
