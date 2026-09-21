using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Domain.ValueObjects;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.MassTransit.Activities.PlayLists
{
    internal class MarkPlayListAsRemovingActivity(
        IDatabase database,
        ILogger<MarkPlayListAsRemovingActivity> logger)
        : IExecuteActivity<MarkPlayListAsRemovingArguments>
    {
        public const string ExecuteEndpointName = "mark-play-list-as-removing";

        public async Task<ExecutionResult> Execute(ExecuteContext<MarkPlayListAsRemovingArguments> executeContext)
        {
            var playList = await database.PlayLists
                .SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.PlayListId, executeContext.CancellationToken);

            if (playList == null)
            {
                logger.LogInformation(
                    "PlayList {PlayListId} not found while marking as removing, skipping",
                    executeContext.Arguments.PlayListId);
                return executeContext.Completed();
            }

            playList.LifeCycleStatus = LifeCycleStatus.Removing;
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Marked playlist {PlayListId} as removing", playList.Id);
            return executeContext.Completed();
        }
    }
}
